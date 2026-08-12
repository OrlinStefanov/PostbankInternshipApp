using BinTool.Application.Abstractions;
using BinTool.Application.Mapping;
using BinTool.Application.Models.Audit;
using BinTool.Application.Models.Commission;
using BinTool.Application.Validation;
using BinTool.Domain.Common;
using Microsoft.Extensions.Logging;

namespace BinTool.Application.Services;

// Every write follows the same beats - validate, resolve the key ids to names, refuse a conflict,
// snapshot, apply, audit in the same unit of work, save. A rule carries exactly one criteria row:
// the schema permits many, but the invariant is held here.
public class CommissionRuleAdminService : ICommissionRuleAdminService
{
    private readonly ICommissionRuleRepository _rules;
    private readonly IReferenceDataRepository _reference;
    private readonly ICurrentUser _currentUser;
    private readonly IAuditLog _audit;
    private readonly ILogger<CommissionRuleAdminService> _logger;

    public CommissionRuleAdminService(
        ICommissionRuleRepository rules,
        IReferenceDataRepository reference,
        ICurrentUser currentUser,
        IAuditLog audit,
        ILogger<CommissionRuleAdminService> logger)
    {
        _rules = rules;
        _reference = reference;
        _currentUser = currentUser;
        _audit = audit;
        _logger = logger;
    }

    public async Task<List<CommissionRuleListItem>> SearchAsync(
        bool includeDeleted, bool includeExpired, CancellationToken cancellationToken = default)
    {
        var defaultRuleId = await _rules.GetDefaultRuleIdAsync(cancellationToken);
        var today = DateTime.UtcNow.Date;

        var rules = await _rules.ListAsync(includeDeleted, cancellationToken);

        return rules
            .Select(r => CommissionRuleMapper.ToListItem(r, defaultRuleId, today))
            .Where(i => includeExpired || i.Status != CommissionRuleStatus.Expired)
            // Active first, then scheduled, then the dormant states; within a band the order
            // is the one resolution uses, so the listing reads top-to-bottom in the order
            // rules actually win. Total, so it does not depend on how rows were entered.
            .OrderBy(i => StatusRank(i.Status))
            .ThenByDescending(i => i.Priority)
            .ThenByDescending(i => i.PriorityScore)
            .ThenBy(i => i.RuleName, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public async Task<CommissionRuleListItem?> GetAsync(
        int id, CancellationToken cancellationToken = default)
    {
        var rule = await _rules.GetWithReferencesAsync(id, cancellationToken);
        if (rule is null) return null;

        var defaultRuleId = await _rules.GetDefaultRuleIdAsync(cancellationToken);

        return CommissionRuleMapper.ToListItem(rule, defaultRuleId, DateTime.UtcNow.Date);
    }

    public async Task<CommissionRuleMutationResult> CreateAsync(
        CommissionRuleInput input, CancellationToken cancellationToken = default)
    {
        var (names, refused) = await PrepareAsync(input, excludeRuleId: 0, cancellationToken);
        if (refused is not null) return Refused(refused, ruleId: 0);

        var now = DateTime.UtcNow;
        var rule = new CommissionRule { CreatedAt = now, CreatedBy = _currentUser.Name };

        CommissionRuleMapper.Apply(rule, input, _currentUser.Name, now);

        var criteria = new RuleCriteria();
        CommissionRuleMapper.ApplyCriteria(criteria, input);
        rule.RuleCriteria.Add(criteria);

        // The rule has no id until it is saved and the audit entry has to carry one, so the
        // two saves are wrapped: a rule that committed without its audit row would be a
        // silent hole in the trail.
        await using var transaction = await _rules.BeginTransactionAsync(cancellationToken);

        _rules.Add(rule);
        await _rules.SaveChangesAsync(cancellationToken);

        _audit.Record(AuditAction.Created, AuditEntityTypes.CommissionRule, rule.CommissionRuleId,
            null, CommissionRuleMapper.ToSnapshot(input, names, isDeleted: false));

        await _rules.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        CommissionRuleLog.Created(_logger,
            rule.CommissionRuleId, rule.RuleName, _currentUser.Name,
            rule.Priority, rule.PriorityScore());

        return await SucceededAsync(
            CommissionRuleMutationStatus.Created, rule.CommissionRuleId, cancellationToken);
    }

    public async Task<CommissionRuleMutationResult> UpdateAsync(
        int id, CommissionRuleInput input, CancellationToken cancellationToken = default)
    {
        var rule = await _rules.GetForUpdateAsync(id, cancellationToken);
        if (rule is null) return NotFound(id);

        if (rule.IsDeleted)
        {
            return Refused(CommissionRuleMutationResult.Failure(
                CommissionRuleMutationStatus.NotFound,
                $"Commission rule {id} is deleted. Restore it before editing."), id);
        }

        var (names, refused) = await PrepareAsync(input, excludeRuleId: id, cancellationToken);
        if (refused is not null) return Refused(refused, id);

        // Read the stored values before they are overwritten; afterwards they are gone.
        var before = await SnapshotAsync(id, cancellationToken);

        CommissionRuleMapper.Apply(rule, input, _currentUser.Name, DateTime.UtcNow);

        // One criteria row per rule: overwrite the existing one, or add it if the row is
        // somehow missing (a rule imported outside this service).
        var criteria = rule.RuleCriteria.FirstOrDefault();
        if (criteria is null)
        {
            criteria = new RuleCriteria { CommissionRuleId = id };
            rule.RuleCriteria.Add(criteria);
        }

        CommissionRuleMapper.ApplyCriteria(criteria, input);

        _audit.Record(AuditAction.Updated, AuditEntityTypes.CommissionRule, id,
            before, CommissionRuleMapper.ToSnapshot(input, names, isDeleted: false));

        await _rules.SaveChangesAsync(cancellationToken);

        CommissionRuleLog.Updated(_logger, id, rule.RuleName, _currentUser.Name);

        return await SucceededAsync(CommissionRuleMutationStatus.Updated, id, cancellationToken);
    }

    public async Task<CommissionRuleMutationResult> DeleteAsync(
        int id, CancellationToken cancellationToken = default)
    {
        var rule = await _rules.GetForUpdateAsync(id, cancellationToken);
        if (rule is null) return NotFound(id);

        if (rule.IsDeleted)
        {
            return Refused(CommissionRuleMutationResult.Failure(
                CommissionRuleMutationStatus.AlreadyInThatState,
                $"Commission rule '{rule.RuleName}' is already deleted."), id);
        }

        if (await _rules.GetDefaultRuleIdAsync(cancellationToken) == id)
        {
            // Deleting the default would leave unmatched classifications with no fallback.
            return Refused(CommissionRuleMutationResult.Failure(
                CommissionRuleMutationStatus.InUse,
                $"Commission rule '{rule.RuleName}' is the default rule. " +
                "Set another rule as the default, or clear it, before deleting this one."), id);
        }

        var now = DateTime.UtcNow;
        var before = await SnapshotAsync(id, cancellationToken);

        // Soft delete: the rule stops taking part in resolution and leaves the default
        // listing, but nothing is erased.
        rule.IsDeleted = true;
        rule.DeletedAt = now;
        rule.DeletedBy = _currentUser.Name;
        rule.UpdatedAt = now;
        rule.UpdatedBy = _currentUser.Name;

        // Nothing but the flag moved, so the two sides differ only there - which is exactly
        // what a reader needs to see.
        _audit.Record(AuditAction.Deleted, AuditEntityTypes.CommissionRule, id,
            before, before with { IsDeleted = true });

        await _rules.SaveChangesAsync(cancellationToken);

        CommissionRuleLog.Deleted(_logger, id, rule.RuleName, _currentUser.Name);

        return await SucceededAsync(CommissionRuleMutationStatus.Deleted, id, cancellationToken);
    }

    public async Task<CommissionRuleMutationResult> RestoreAsync(
        int id, CancellationToken cancellationToken = default)
    {
        var rule = await _rules.GetForUpdateAsync(id, cancellationToken);
        if (rule is null) return NotFound(id);

        if (!rule.IsDeleted)
        {
            return Refused(CommissionRuleMutationResult.Failure(
                CommissionRuleMutationStatus.AlreadyInThatState,
                $"Commission rule '{rule.RuleName}' is not deleted."), id);
        }

        var now = DateTime.UtcNow;
        var before = await SnapshotAsync(id, cancellationToken);

        rule.IsDeleted = false;
        rule.DeletedAt = null;
        rule.DeletedBy = null;
        rule.UpdatedAt = now;
        rule.UpdatedBy = _currentUser.Name;

        _audit.Record(AuditAction.Updated, AuditEntityTypes.CommissionRule, id,
            before, before with { IsDeleted = false });

        await _rules.SaveChangesAsync(cancellationToken);

        CommissionRuleLog.Restored(_logger, id, rule.RuleName, _currentUser.Name);

        return await SucceededAsync(CommissionRuleMutationStatus.Restored, id, cancellationToken);
    }

    public async Task<CommissionRuleMutationResult> SetDefaultAsync(
        int id, CancellationToken cancellationToken = default)
    {
        var rule = await _rules.GetForUpdateAsync(id, cancellationToken);
        if (rule is null) return NotFound(id);

        if (rule.IsDeleted || !rule.IsActive)
        {
            return Refused(CommissionRuleMutationResult.Failure(
                CommissionRuleMutationStatus.Invalid,
                $"Commission rule '{rule.RuleName}' must be active and not deleted to be the default."), id);
        }

        var existing = await _rules.GetDefaultAsync(cancellationToken);
        var before = await DefaultSnapshotAsync(existing?.CommissionRuleId, cancellationToken);

        if (existing is not null && existing.CommissionRuleId == id)
        {
            // Already the default - nothing to change, but report success with the rule.
            return await SucceededAsync(CommissionRuleMutationStatus.Updated, id, cancellationToken);
        }

        await using var transaction = await _rules.BeginTransactionAsync(cancellationToken);

        if (existing is null)
        {
            existing = new DefaultRule { CommissionRuleId = id, IsSystemDefault = true };
            _rules.AddDefault(existing);
        }
        else
        {
            existing.CommissionRuleId = id;
        }

        await _rules.SaveChangesAsync(cancellationToken);

        _audit.Record(AuditAction.Updated, AuditEntityTypes.DefaultRule, existing.DefaultRuleId,
            before, new DefaultRuleSnapshot(id, rule.RuleName));

        await _rules.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        CommissionRuleLog.DefaultSet(_logger, id, rule.RuleName, _currentUser.Name);

        return await SucceededAsync(CommissionRuleMutationStatus.Updated, id, cancellationToken);
    }

    public async Task<CommissionRuleMutationResult> ClearDefaultAsync(
        CancellationToken cancellationToken = default)
    {
        var existing = await _rules.GetDefaultAsync(cancellationToken);

        // Clearing an already-clear default is a harmless no-op that still reports success.
        if (existing is null)
        {
            return new CommissionRuleMutationResult { Status = CommissionRuleMutationStatus.Updated };
        }

        var before = await DefaultSnapshotAsync(existing.CommissionRuleId, cancellationToken);
        var entityId = existing.DefaultRuleId;

        _rules.RemoveDefault(existing);

        _audit.Record(AuditAction.Updated, AuditEntityTypes.DefaultRule, entityId,
            before, new DefaultRuleSnapshot(null, null));

        await _rules.SaveChangesAsync(cancellationToken);

        // Worth an event of its own: with no default, a card that matches no rule stops
        // being priced at all, and that is a configuration change someone should be able
        // to find later without reading the audit table.
        CommissionRuleLog.DefaultCleared(_logger, _currentUser.Name);

        return new CommissionRuleMutationResult { Status = CommissionRuleMutationStatus.Updated };
    }

    // ---- The shared write preamble ---------------------------------------------

    // Everything a create and an update both have to establish before they may proceed: the input
    // is well formed, its key ids resolve to live reference rows, and saving it would leave
    // resolution unambiguous. Returns the resolved names, or the refusal.
    private async Task<(ReferenceNames Names, CommissionRuleMutationResult? Refused)> PrepareAsync(
        CommissionRuleInput input, int excludeRuleId, CancellationToken cancellationToken)
    {
        if (!CommissionRuleValidator.TryValidate(input, out var error))
        {
            return (default, CommissionRuleMutationResult.Failure(
                CommissionRuleMutationStatus.Invalid, error));
        }

        var (names, unresolved) = await ResolveKeyAsync(input, cancellationToken);
        if (unresolved is not null) return (default, unresolved);

        var overlap = await FindKeyOverlapAsync(input, excludeRuleId, cancellationToken);
        if (overlap is not null) return (default, overlap);

        var ambiguity = await FindAmbiguityAsync(input, excludeRuleId, cancellationToken);
        if (ambiguity is not null) return (default, ambiguity);

        return (names, null);
    }

    // Confirms each supplied key id belongs to a live reference row. A null id is a wildcard and
    // resolves to no name, so a name that comes back null for an id that was supplied is the one
    // that went missing. All of them are reported at once.
    private async Task<(ReferenceNames Names, CommissionRuleMutationResult? Failure)> ResolveKeyAsync(
        CommissionRuleInput input, CancellationToken cancellationToken)
    {
        var key = input.Key();
        var names = await _reference.ResolveAsync(input.CurrencyId, key, cancellationToken);

        var missing = new List<string>();

        // The currency is required, not a wildcard, so it is always expected to resolve.
        if (names.Currency is null) missing.Add($"Currency {input.CurrencyId} does not exist.");

        if (key.CardSchemeId is { } schemeId && names.CardScheme is null)
        {
            missing.Add($"Card scheme {schemeId} does not exist.");
        }

        if (key.ProductTypeId is { } productId && names.ProductType is null)
        {
            missing.Add($"Product type {productId} does not exist.");
        }

        if (key.FundingTypeId is { } fundingId && names.FundingType is null)
        {
            missing.Add($"Funding type {fundingId} does not exist.");
        }

        if (key.RegionId is { } regionId && names.Region is null)
        {
            missing.Add($"Region {regionId} does not exist.");
        }

        if (missing.Count > 0)
        {
            return (default, CommissionRuleMutationResult.Failure(
                CommissionRuleMutationStatus.Invalid, string.Join(" ", missing)));
        }

        return (names, null);
    }

    // Refuses a second rule with an identical key covering the same days. Two rules that say
    // different things about exactly the same cards at the same time are duplicate tariffs
    // regardless of how they are ranked.
    private async Task<CommissionRuleMutationResult?> FindKeyOverlapAsync(
        CommissionRuleInput input, int excludeRuleId, CancellationToken cancellationToken)
    {
        var conflict = await _rules.FindKeyOverlapAsync(
            input.Key(), input.Validity(), excludeRuleId, cancellationToken);

        if (conflict is not { } other) return null;

        return CommissionRuleMutationResult.Conflict(
            $"This rule's validity overlaps rule '{other.Name}', which already covers " +
            "the same scheme, product, funding and region for part of that period.",
            other.Id, other.Name);
    }

    // Refuses a rule that would tie with another one on every tiebreak resolution has. The database
    // narrows to rules sharing the priority and touching the window; the rest - equal score, keys
    // that can match the same card - is decided here, because it is a domain rule and reads as one.
    private async Task<CommissionRuleMutationResult?> FindAmbiguityAsync(
        CommissionRuleInput input, int excludeRuleId, CancellationToken cancellationToken)
    {
        var key = input.Key();
        var score = input.EffectivePriorityScore();

        var candidates = await _rules.FindPriorityCandidatesAsync(
            input.Priority, input.Validity(), excludeRuleId, cancellationToken);

        var clash = candidates.FirstOrDefault(
            other => other.PriorityScore() == score && key.IsCoMatchableWith(other.Key()));

        if (clash is null) return null;

        return CommissionRuleMutationResult.Conflict(
            $"Rule '{clash.RuleName}' has the same priority ({input.Priority}) and score " +
            $"({score}), and its criteria can match the same cards during an overlapping " +
            "period. Change the priority or score so the two rules are unambiguous.",
            clash.CommissionRuleId, clash.RuleName);
    }

    // ---- Small shared pieces ----------------------------------------------------

    private async Task<CommissionRuleSnapshot> SnapshotAsync(
        int id, CancellationToken cancellationToken)
    {
        var rule = await _rules.GetWithReferencesAsync(id, cancellationToken);
        return CommissionRuleMapper.ToSnapshot(rule!);
    }

    private async Task<DefaultRuleSnapshot> DefaultSnapshotAsync(
        int? commissionRuleId, CancellationToken cancellationToken)
    {
        if (commissionRuleId is not { } ruleId) return new DefaultRuleSnapshot(null, null);

        var name = await _rules.GetRuleNameAsync(ruleId, cancellationToken);
        return new DefaultRuleSnapshot(ruleId, name);
    }

    private async Task<CommissionRuleMutationResult> SucceededAsync(
        CommissionRuleMutationStatus status, int id, CancellationToken cancellationToken)
    {
        var item = await GetAsync(id, cancellationToken);
        return CommissionRuleMutationResult.Success(status, item!);
    }

    // Logs a refusal on its way out. Every refused write goes through here, so the log cannot drift
    // out of step with what the caller was told - the reason logged is the same string the user
    // reads.
    private CommissionRuleMutationResult Refused(CommissionRuleMutationResult result, int ruleId)
    {
        var reason = result.Error ?? string.Empty;

        switch (result.Status)
        {
            case CommissionRuleMutationStatus.Overlap:
                CommissionRuleLog.RefusedAsConflicting(_logger,
                    result.ConflictingRuleId ?? 0, result.ConflictingRuleName ?? "(unnamed)", reason);
                break;

            case CommissionRuleMutationStatus.Invalid:
                CommissionRuleLog.RefusedAsInvalid(_logger, reason);
                break;

            default:
                CommissionRuleLog.RefusedAsUnavailable(_logger, ruleId, reason);
                break;
        }

        return result;
    }

    private CommissionRuleMutationResult NotFound(int id) =>
        Refused(CommissionRuleMutationResult.Failure(
            CommissionRuleMutationStatus.NotFound, $"No commission rule with id {id}."), id);

    private static int StatusRank(CommissionRuleStatus status) => status switch
    {
        CommissionRuleStatus.Active => 0,
        CommissionRuleStatus.Scheduled => 1,
        CommissionRuleStatus.Inactive => 2,
        CommissionRuleStatus.Expired => 3,
        CommissionRuleStatus.Deleted => 4,
        _ => 5
    };
}
