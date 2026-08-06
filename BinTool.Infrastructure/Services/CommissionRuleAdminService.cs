using System.ComponentModel.DataAnnotations;
using BinTool.Core.Entities;
using BinTool.Core.Models.Audit;
using BinTool.Core.Models.Commission;
using BinTool.Core.Services;
using BinTool.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BinTool.Infrastructure.Services;

/// <summary>
/// Maintains commission rules. Follows the same beats as <see cref="BinRangeAdminService"/>:
/// validate, resolve the key ids to names, snapshot the row before it changes, apply, audit
/// in the same unit of work, and save. A rule carries exactly one criteria row - the schema
/// permits many, but a rule keyed on a single scheme/product/region combination is what the
/// domain calls for, so the service holds that invariant.
/// </summary>
public class CommissionRuleAdminService : ICommissionRuleAdminService
{
    private readonly AppDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IAuditLog _audit;

    public CommissionRuleAdminService(AppDbContext db, ICurrentUser currentUser, IAuditLog audit)
    {
        _db = db;
        _currentUser = currentUser;
        _audit = audit;
    }

    public async Task<List<CommissionRuleListItem>> SearchAsync(
        bool includeDeleted, bool includeExpired, CancellationToken cancellationToken = default)
    {
        var defaultRuleId = await CurrentDefaultRuleIdAsync(cancellationToken);
        var today = DateTime.UtcNow.Date;

        var query = WithReferences(_db.CommissionRules.AsNoTracking());
        if (!includeDeleted) query = query.Where(r => !r.IsDeleted);

        var rules = await query.ToListAsync(cancellationToken);

        var items = rules
            .Select(r => ToListItem(r, defaultRuleId, today))
            .Where(i => includeExpired || i.Status != CommissionRuleStatus.Expired)
            // Active first, then scheduled, then the various dormant states; within a band
            // the most specific rule leads, then the manual priority, then the name. The
            // ordering is total, so a listing is stable however the rows were entered.
            .OrderBy(i => StatusRank(i.Status))
            .ThenByDescending(i => i.Specificity)
            .ThenByDescending(i => i.Priority)
            .ThenBy(i => i.RuleName, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return items;
    }

    public async Task<CommissionRuleListItem?> GetAsync(
        int id, CancellationToken cancellationToken = default)
    {
        var defaultRuleId = await CurrentDefaultRuleIdAsync(cancellationToken);
        var today = DateTime.UtcNow.Date;

        var rule = await WithReferences(_db.CommissionRules.AsNoTracking())
            .FirstOrDefaultAsync(r => r.CommissionRuleId == id, cancellationToken);

        return rule is null ? null : ToListItem(rule, defaultRuleId, today);
    }

    public async Task<CommissionRuleMutationResult> CreateAsync(
        CommissionRuleInput input, CancellationToken cancellationToken = default)
    {
        if (!TryValidate(input, out var invalid)) return invalid;

        var (resolved, unresolved) = await ResolveKeyAsync(input, cancellationToken);
        if (unresolved is not null) return unresolved;

        var overlap = await FindOverlapAsync(input, excludeRuleId: 0, cancellationToken);
        if (overlap is not null) return overlap;

        var now = DateTime.UtcNow;

        var rule = new CommissionRule
        {
            CreatedAt = now,
            CreatedBy = _currentUser.Name
        };

        Apply(rule, input, now);
        
        rule.RuleCriteria.Add(new RuleCriteria
        {
            CardSchemeId = input.CardSchemeId,
            ProductTypeId = input.ProductTypeId,
            FundingTypeId = input.FundingTypeId,
            RegionId = input.RegionId,
            PriorityScore = Specificity(input)
        });

        // The rule has no id until it is saved, and the audit entry has to carry one - so
        // the two saves are wrapped in a transaction, exactly as the BIN-range insert is.
        await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);

        _db.CommissionRules.Add(rule);
        
        await _db.SaveChangesAsync(cancellationToken);

        _audit.Record(AuditAction.Created, AuditEntityTypes.CommissionRule, rule.CommissionRuleId,
            null, SnapshotFromInput(input, resolved, isDeleted: false));

        await _db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return await SucceededAsync(
            CommissionRuleMutationStatus.Created, rule.CommissionRuleId, cancellationToken);
    }

    public async Task<CommissionRuleMutationResult> UpdateAsync(
        int id, CommissionRuleInput input, CancellationToken cancellationToken = default)
    {
        if (!TryValidate(input, out var invalid)) return invalid;

        var rule = await _db.CommissionRules
            .Include(r => r.RuleCriteria)
            .FirstOrDefaultAsync(r => r.CommissionRuleId == id, cancellationToken);

        if (rule is null) return NotFound(id);

        if (rule.IsDeleted)
        {
            return CommissionRuleMutationResult.Failure(
                CommissionRuleMutationStatus.NotFound,
                $"Commission rule {id} is deleted. Restore it before editing.");
        }

        var (resolved, unresolved) = await ResolveKeyAsync(input, cancellationToken);

        if (unresolved is not null) return unresolved;

        var overlap = await FindOverlapAsync(input, excludeRuleId: id, cancellationToken);

        if (overlap is not null) return overlap;

        var before = await SnapshotAsync(id, cancellationToken);

        Apply(rule, input, DateTime.UtcNow);

        // One criteria row per rule: overwrite the existing one, or add it if the row is
        // somehow missing (a rule imported outside this service).
        var criteria = rule.RuleCriteria.FirstOrDefault();

        if (criteria is null)
        {
            criteria = new RuleCriteria { CommissionRuleId = id };
            rule.RuleCriteria.Add(criteria);
        }

        criteria.CardSchemeId = input.CardSchemeId;
        criteria.ProductTypeId = input.ProductTypeId;
        criteria.FundingTypeId = input.FundingTypeId;
        criteria.RegionId = input.RegionId;
        criteria.PriorityScore = Specificity(input);

        _audit.Record(AuditAction.Updated, AuditEntityTypes.CommissionRule, id,
            before, SnapshotFromInput(input, resolved, isDeleted: false));

        await _db.SaveChangesAsync(cancellationToken);

        return await SucceededAsync(CommissionRuleMutationStatus.Updated, id, cancellationToken);
    }

    public async Task<CommissionRuleMutationResult> DeleteAsync(
        int id, CancellationToken cancellationToken = default)
    {
        var rule = await _db.CommissionRules
            .FirstOrDefaultAsync(r => r.CommissionRuleId == id, cancellationToken);

        if (rule is null) return NotFound(id);

        if (rule.IsDeleted)
        {
            return CommissionRuleMutationResult.Failure(
                CommissionRuleMutationStatus.AlreadyInThatState,
                $"Commission rule '{rule.RuleName}' is already deleted.");
        }

        var defaultRuleId = await CurrentDefaultRuleIdAsync(cancellationToken);

        if (defaultRuleId == id)
        {
            // Deleting the default would leave unmatched classifications with no fallback.
            return CommissionRuleMutationResult.Failure(
                CommissionRuleMutationStatus.InUse,
                $"Commission rule '{rule.RuleName}' is the default rule. " +
                "Set another rule as the default, or clear it, before deleting this one.");
        }

        var now = DateTime.UtcNow;
        var before = await SnapshotAsync(id, cancellationToken);

        rule.IsDeleted = true;
        rule.DeletedAt = now;
        rule.DeletedBy = _currentUser.Name;
        rule.UpdatedAt = now;
        rule.UpdatedBy = _currentUser.Name;

        _audit.Record(AuditAction.Deleted, AuditEntityTypes.CommissionRule, id,
            before, before with { IsDeleted = true });

        await _db.SaveChangesAsync(cancellationToken);

        return await SucceededAsync(CommissionRuleMutationStatus.Deleted, id, cancellationToken);
    }

    public async Task<CommissionRuleMutationResult> RestoreAsync(
        int id, CancellationToken cancellationToken = default)
    {
        var rule = await _db.CommissionRules
            .FirstOrDefaultAsync(r => r.CommissionRuleId == id, cancellationToken);

        if (rule is null) return NotFound(id);

        if (!rule.IsDeleted)
        {
            return CommissionRuleMutationResult.Failure(
                CommissionRuleMutationStatus.AlreadyInThatState,
                $"Commission rule '{rule.RuleName}' is not deleted.");
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

        await _db.SaveChangesAsync(cancellationToken);

        return await SucceededAsync(CommissionRuleMutationStatus.Restored, id, cancellationToken);
    }

    public async Task<CommissionRuleMutationResult> SetDefaultAsync(
        int id, CancellationToken cancellationToken = default)
    {
        var rule = await _db.CommissionRules
            .FirstOrDefaultAsync(r => r.CommissionRuleId == id, cancellationToken);

        if (rule is null) return NotFound(id);

        if (rule.IsDeleted || !rule.IsActive)
        {
            return CommissionRuleMutationResult.Failure(
                CommissionRuleMutationStatus.Invalid,
                $"Commission rule '{rule.RuleName}' must be active and not deleted to be the default.");
        }

        var existing = await _db.DefaultRules.FirstOrDefaultAsync(cancellationToken);

        var before = await DefaultSnapshotAsync(existing?.CommissionRuleId, cancellationToken);

        if (existing is not null && existing.CommissionRuleId == id)
        {
            // Already the default - nothing to change, but report success with the rule.
            return await SucceededAsync(CommissionRuleMutationStatus.Updated, id, cancellationToken);
        }

        await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);

        if (existing is null)
        {
            existing = new DefaultRule { CommissionRuleId = id, IsSystemDefault = true };
            _db.DefaultRules.Add(existing);
        }
        else
        {
            existing.CommissionRuleId = id;
        }

        await _db.SaveChangesAsync(cancellationToken);

        _audit.Record(AuditAction.Updated, AuditEntityTypes.DefaultRule, existing.DefaultRuleId,
            before, new DefaultRuleSnapshot(id, rule.RuleName));

        await _db.SaveChangesAsync(cancellationToken);

        await transaction.CommitAsync(cancellationToken);

        return await SucceededAsync(CommissionRuleMutationStatus.Updated, id, cancellationToken);
    }

    public async Task<CommissionRuleMutationResult> ClearDefaultAsync(
        CancellationToken cancellationToken = default)
    {
        var existing = await _db.DefaultRules.FirstOrDefaultAsync(cancellationToken);

        // Clearing an already-clear default is a harmless no-op that still reports success.
        if (existing is null)
        {
            return new CommissionRuleMutationResult { Status = CommissionRuleMutationStatus.Updated };
        }

        var before = await DefaultSnapshotAsync(existing.CommissionRuleId, cancellationToken);
        var entityId = existing.DefaultRuleId;

        _db.DefaultRules.Remove(existing);

        _audit.Record(AuditAction.Updated, AuditEntityTypes.DefaultRule, entityId,
            before, new DefaultRuleSnapshot(null, null));

        await _db.SaveChangesAsync(cancellationToken);

        return new CommissionRuleMutationResult { Status = CommissionRuleMutationStatus.Updated };
    }

    // ---- Helpers --------------------------------------------------------------

    /// <summary>Writes the scalar values onto a rule, so no field is silently left behind.</summary>
    private void Apply(CommissionRule rule, CommissionRuleInput input, DateTime now)
    {
        rule.RuleName = input.RuleName.Trim();
        rule.PercentageRate = input.PercentageRate;
        rule.FixedAmount = input.FixedAmount;
        rule.MinimumFee = input.MinimumFee;
        rule.Priority = input.Priority;
        rule.ValidFrom = input.ValidFrom.Date;
        rule.ValidTo = input.ValidTo?.Date;
        rule.IsActive = input.IsActive;
        rule.UpdatedAt = now;
        rule.UpdatedBy = _currentUser.Name;
    }

    /// <summary>Loads a rule with its criteria and the reference names those ids resolve to.</summary>
    private static IQueryable<CommissionRule> WithReferences(IQueryable<CommissionRule> query) =>
        query
            .Include(r => r.RuleCriteria).ThenInclude(c => c.CardScheme)
            .Include(r => r.RuleCriteria).ThenInclude(c => c.ProductType)
            .Include(r => r.RuleCriteria).ThenInclude(c => c.FundingType)
            .Include(r => r.RuleCriteria).ThenInclude(c => c.Region);

    private static CommissionRuleListItem ToListItem(
        CommissionRule rule, int? defaultRuleId, DateTime today)
    {
        var criteria = rule.RuleCriteria.FirstOrDefault();

        return new CommissionRuleListItem
        {
            Id = rule.CommissionRuleId,
            RuleName = rule.RuleName,
            CardSchemeId = criteria?.CardSchemeId,
            CardSchemeName = criteria?.CardScheme?.Name,
            ProductTypeId = criteria?.ProductTypeId,
            ProductTypeName = criteria?.ProductType?.Name,
            FundingTypeId = criteria?.FundingTypeId,
            FundingTypeName = criteria?.FundingType?.Name,
            RegionId = criteria?.RegionId,
            RegionName = criteria?.Region?.Name,
            PercentageRate = rule.PercentageRate,
            FixedAmount = rule.FixedAmount,
            MinimumFee = rule.MinimumFee,
            Priority = rule.Priority,
            Specificity = criteria?.PriorityScore ?? 0,
            ValidFrom = rule.ValidFrom,
            ValidTo = rule.ValidTo,
            IsActive = rule.IsActive,
            Status = DeriveStatus(rule, today),
            IsDefault = defaultRuleId == rule.CommissionRuleId,
            CreatedAt = rule.CreatedAt,
            CreatedBy = rule.CreatedBy,
            UpdatedAt = rule.UpdatedAt,
            UpdatedBy = rule.UpdatedBy,
            DeletedAt = rule.DeletedAt,
            DeletedBy = rule.DeletedBy
        };
    }

    private static CommissionRuleStatus DeriveStatus(CommissionRule rule, DateTime today)
    {
        if (rule.IsDeleted) return CommissionRuleStatus.Deleted;
        if (!rule.IsActive) return CommissionRuleStatus.Inactive;
        if (rule.ValidFrom.Date > today) return CommissionRuleStatus.Scheduled;
        if (rule.ValidTo is { } to && to.Date < today) return CommissionRuleStatus.Expired;

        return CommissionRuleStatus.Active;
    }

    private static int StatusRank(CommissionRuleStatus status) => status switch
    {
        CommissionRuleStatus.Active => 0,
        CommissionRuleStatus.Scheduled => 1,
        CommissionRuleStatus.Inactive => 2,
        CommissionRuleStatus.Expired => 3,
        CommissionRuleStatus.Deleted => 4,
        _ => 5
    };

    private static int Specificity(CommissionRuleInput input) =>
        (input.CardSchemeId is null ? 0 : 1)
        + (input.ProductTypeId is null ? 0 : 1)
        + (input.FundingTypeId is null ? 0 : 1)
        + (input.RegionId is null ? 0 : 1);

    private async Task<int?> CurrentDefaultRuleIdAsync(CancellationToken cancellationToken) =>
        await _db.DefaultRules.AsNoTracking()
            .Select(d => (int?)d.CommissionRuleId)
            .FirstOrDefaultAsync(cancellationToken);

    /// <summary>
    /// Confirms each supplied key id belongs to a live reference row and returns the names,
    /// all missing ones reported at once. A null id is a wildcard and resolves to no name.
    /// </summary>
    private async Task<(ResolvedKey Resolved, CommissionRuleMutationResult? Failure)> ResolveKeyAsync(
        CommissionRuleInput input, CancellationToken cancellationToken)
    {
        string? schemeName = null, productName = null, fundingName = null, regionName = null;
        var missing = new List<string>();

        if (input.CardSchemeId is { } schemeId)
        {
            schemeName = await _db.CardSchemes.AsNoTracking()
                .Where(x => !x.IsDeleted && x.CardSchemeId == schemeId)
                .Select(x => x.Name).FirstOrDefaultAsync(cancellationToken);
            if (schemeName is null) missing.Add($"Card scheme {schemeId} does not exist.");
        }

        if (input.ProductTypeId is { } productId)
        {
            productName = await _db.ProductTypes.AsNoTracking()
                .Where(x => !x.IsDeleted && x.ProductTypeId == productId)
                .Select(x => x.Name).FirstOrDefaultAsync(cancellationToken);
            if (productName is null) missing.Add($"Product type {productId} does not exist.");
        }

        if (input.FundingTypeId is { } fundingId)
        {
            fundingName = await _db.FundingTypes.AsNoTracking()
                .Where(x => !x.IsDeleted && x.FundingTypeId == fundingId)
                .Select(x => x.Name).FirstOrDefaultAsync(cancellationToken);
            if (fundingName is null) missing.Add($"Funding type {fundingId} does not exist.");
        }

        if (input.RegionId is { } regionId)
        {
            regionName = await _db.Regions.AsNoTracking()
                .Where(x => !x.IsDeleted && x.RegionId == regionId)
                .Select(x => x.Name).FirstOrDefaultAsync(cancellationToken);
            if (regionName is null) missing.Add($"Region {regionId} does not exist.");
        }

        if (missing.Count > 0)
        {
            return (default, CommissionRuleMutationResult.Failure(
                CommissionRuleMutationStatus.Invalid, string.Join(" ", missing)));
        }

        return (new ResolvedKey(schemeName, productName, fundingName, regionName), null);
    }

    /// <summary>
    /// Looks for a live rule with the same scheme/product/region key whose validity window
    /// touches this one. Inclusive dates, open-ended treated as infinity. Returns the refusal
    /// naming the first conflicting rule, or null when the window is clear.
    /// </summary>
    private async Task<CommissionRuleMutationResult?> FindOverlapAsync(
        CommissionRuleInput input, int excludeRuleId, CancellationToken cancellationToken)
    {
        var inputTo = input.ValidTo?.Date ?? DateTime.MaxValue;
        var inputFrom = input.ValidFrom.Date;

        var conflict = await _db.CommissionRules.AsNoTracking()
            .Where(r => !r.IsDeleted && r.CommissionRuleId != excludeRuleId)
            .Where(r => r.RuleCriteria.Any(c =>
                c.CardSchemeId == input.CardSchemeId
                && c.ProductTypeId == input.ProductTypeId
                && c.FundingTypeId == input.FundingTypeId
                && c.RegionId == input.RegionId))
            // Inclusive overlap: r.From <= input.To AND input.From <= r.To.
            .Where(r => r.ValidFrom <= inputTo
                && inputFrom <= (r.ValidTo ?? DateTime.MaxValue))
            .OrderBy(r => r.ValidFrom)
            .Select(r => new { r.CommissionRuleId, r.RuleName })
            .FirstOrDefaultAsync(cancellationToken);

        if (conflict is null) return null;

        return CommissionRuleMutationResult.Conflict(
            $"This rule's validity overlaps rule '{conflict.RuleName}', which already covers " +
            "the same scheme, product, funding and region for part of that period.",
            conflict.CommissionRuleId, conflict.RuleName);
    }

    private async Task<CommissionRuleSnapshot> SnapshotAsync(int id, CancellationToken cancellationToken)
    {
        var rule = await WithReferences(_db.CommissionRules.AsNoTracking())
            .FirstAsync(r => r.CommissionRuleId == id, cancellationToken);

        var criteria = rule.RuleCriteria.FirstOrDefault();

        return new CommissionRuleSnapshot(
            rule.RuleName,
            criteria?.CardScheme?.Name ?? CommissionRuleSnapshot.AnyValue,
            criteria?.ProductType?.Name ?? CommissionRuleSnapshot.AnyValue,
            criteria?.FundingType?.Name ?? CommissionRuleSnapshot.AnyValue,
            criteria?.Region?.Name ?? CommissionRuleSnapshot.AnyValue,
            rule.PercentageRate,
            rule.FixedAmount,
            rule.MinimumFee,
            rule.Priority,
            CommissionRuleSnapshot.Date(rule.ValidFrom),
            rule.ValidTo is null ? null : CommissionRuleSnapshot.Date(rule.ValidTo.Value),
            rule.IsActive,
            rule.IsDeleted);
    }

    private static CommissionRuleSnapshot SnapshotFromInput(
        CommissionRuleInput input, ResolvedKey resolved, bool isDeleted) =>
        new(input.RuleName.Trim(),
            resolved.CardScheme ?? CommissionRuleSnapshot.AnyValue,
            resolved.ProductType ?? CommissionRuleSnapshot.AnyValue,
            resolved.FundingType ?? CommissionRuleSnapshot.AnyValue,
            resolved.Region ?? CommissionRuleSnapshot.AnyValue,
            input.PercentageRate,
            input.FixedAmount,
            input.MinimumFee,
            input.Priority,
            CommissionRuleSnapshot.Date(input.ValidFrom.Date),
            input.ValidTo is null ? null : CommissionRuleSnapshot.Date(input.ValidTo.Value.Date),
            input.IsActive,
            isDeleted);

    private async Task<DefaultRuleSnapshot> DefaultSnapshotAsync(
        int? commissionRuleId, CancellationToken cancellationToken)
    {
        if (commissionRuleId is not { } ruleId) return new DefaultRuleSnapshot(null, null);

        var name = await _db.CommissionRules.AsNoTracking()
            .Where(r => r.CommissionRuleId == ruleId)
            .Select(r => r.RuleName)
            .FirstOrDefaultAsync(cancellationToken);

        return new DefaultRuleSnapshot(ruleId, name);
    }

    private async Task<CommissionRuleMutationResult> SucceededAsync(
        CommissionRuleMutationStatus status, int id, CancellationToken cancellationToken)
    {
        var item = await GetAsync(id, cancellationToken);
        return CommissionRuleMutationResult.Success(status, item!);
    }

    private static CommissionRuleMutationResult NotFound(int id) =>
        CommissionRuleMutationResult.Failure(
            CommissionRuleMutationStatus.NotFound, $"No commission rule with id {id}.");

    private static bool TryValidate(CommissionRuleInput input, out CommissionRuleMutationResult failure)
    {
        var results = new List<ValidationResult>();
        var messages = new List<string>();

        if (!Validator.TryValidateObject(
                input, new ValidationContext(input), results, validateAllProperties: true))
        {
            messages.AddRange(results.Select(r => r.ErrorMessage!));
        }

        // A cross-field rule the annotations cannot express: an end before the start.
        if (input.ValidTo is { } to && to.Date < input.ValidFrom.Date)
        {
            messages.Add("ValidTo cannot be earlier than ValidFrom.");
        }

        if (messages.Count > 0)
        {
            failure = CommissionRuleMutationResult.Failure(
                CommissionRuleMutationStatus.Invalid, string.Join(" ", messages));
            return false;
        }

        failure = null!;
        return true;
    }

    private readonly record struct ResolvedKey(
        string? CardScheme, string? ProductType, string? FundingType, string? Region);
}
