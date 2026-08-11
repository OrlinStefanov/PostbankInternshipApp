using BinTool.Application.Abstractions;
using BinTool.Application.Mapping;
using BinTool.Application.Models.Audit;
using BinTool.Application.Models.BinRanges;
using BinTool.Application.Validation;
using Microsoft.Extensions.Logging;

namespace BinTool.Application.Services;

public class BinRangeAdminService : IBinRangeAdminService
{
    private readonly IBinRangeRepository _ranges;
    private readonly ICurrentUser _currentUser;
    private readonly IAuditLog _audit;
    private readonly ICardSchemeDetector _schemeDetector;
    private readonly ILogger<BinRangeAdminService> _logger;

    public BinRangeAdminService(
        IBinRangeRepository ranges,
        ICurrentUser currentUser,
        IAuditLog audit,
        ICardSchemeDetector schemeDetector,
        ILogger<BinRangeAdminService> logger)
    {
        _ranges = ranges;
        _currentUser = currentUser;
        _audit = audit;
        _schemeDetector = schemeDetector;
        _logger = logger;
    }

    public Task<BinRangeListItem?> GetAsync(
        int binRangeId, CancellationToken cancellationToken = default) =>
        _ranges.GetAsync(binRangeId, DateTime.UtcNow.Date, cancellationToken);

    public async Task<BinRangeMutationResult> CreateAsync(
        BinRangeInput input, CancellationToken cancellationToken = default)
    {
        var prefix = input.NormalizedPrefix();

        var (resolved, refused) = await PrepareAsync(input, cancellationToken);
        if (refused is not null) return refused;

        var existing = await _ranges.FindByPrefixAsync(prefix, cancellationToken);

        if (existing is { IsDeleted: false })
        {
            return Refused(BinRangeMutationResult.Failure(
                BinRangeMutationStatus.PrefixInUse,
                $"Prefix '{prefix}' already belongs to another BIN range."), prefix);
        }

        // Held in reserve: an admin can accept the row anyway (co-brand block, new
        // allocation the detector does not know) but never by accident.
        if (SchemeMismatch(input, resolved!.Value) is { } mismatch) return mismatch;

        // The unique index on Prefix spans soft-deleted rows, so a deleted range would
        // block the insert. Revive it with the new values instead - the same rule the CSV
        // import follows, and it keeps the row's id and audit trail.
        if (existing is not null)
        {
            return await ReviveAsync(existing, input, resolved.Value, cancellationToken);
        }

        var now = DateTime.UtcNow;
        var range = new BinRange { CreatedAt = now, CreatedBy = _currentUser.Name };

        BinRangeMapper.Apply(range, input, resolved!.Value, _currentUser.Name, now);

        // An insert has no id until it is saved, and the audit entry has to carry one. The
        // transaction keeps the pair atomic across the two saves - a range that committed
        // without its audit row would be a silent hole in the trail.
        await using var transaction = await _ranges.BeginTransactionAsync(cancellationToken);

        _ranges.Add(range);
        await _ranges.SaveChangesAsync(cancellationToken);

        _audit.Record(AuditAction.Created, AuditEntityTypes.BinRange, range.BinRangeId,
            null, BinRangeMapper.ToSnapshot(input, resolved.Value, isDeleted: false));

        await _ranges.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        BinRangeLog.Created(
            _logger, range.BinRangeId, prefix, resolved.Value.CardScheme.Name, _currentUser.Name);

        return await SucceededAsync(
            BinRangeMutationStatus.Created, range.BinRangeId, cancellationToken);
    }

    public async Task<BinRangeMutationResult> UpdateAsync(
        int binRangeId, BinRangeInput input, CancellationToken cancellationToken = default)
    {
        var range = await _ranges.GetForUpdateAsync(binRangeId, cancellationToken);
        if (range is null) return NotFound(binRangeId);

        if (range.IsDeleted)
        {
            // Editing a deleted range would quietly resurrect it as a side effect.
            return Refused(BinRangeMutationResult.Failure(
                BinRangeMutationStatus.NotFound,
                $"BIN range {binRangeId} is deleted. Restore it before editing."), range.Prefix);
        }

        var prefix = input.NormalizedPrefix();

        var (resolved, refused) = await PrepareAsync(input, cancellationToken);
        if (refused is not null) return refused;

        if (prefix != range.Prefix
            && await _ranges.PrefixBelongsToAnotherAsync(prefix, binRangeId, cancellationToken))
        {
            return Refused(BinRangeMutationResult.Failure(
                BinRangeMutationStatus.PrefixInUse,
                $"Prefix '{prefix}' already belongs to another BIN range."), prefix);
        }

        // Same rule as on insert: the network the digits belong to has to match the name
        // being saved, or the caller has to say they know and want it anyway.
        if (SchemeMismatch(input, resolved!.Value) is { } mismatch) return mismatch;

        // Read the stored values before they are overwritten - afterwards they are gone.
        var before = await SnapshotAsync(binRangeId, cancellationToken);

        BinRangeMapper.Apply(range, input, resolved.Value, _currentUser.Name, DateTime.UtcNow);

        _audit.Record(AuditAction.Updated, AuditEntityTypes.BinRange, binRangeId,
            before, BinRangeMapper.ToSnapshot(input, resolved.Value, isDeleted: false));

        await _ranges.SaveChangesAsync(cancellationToken);

        BinRangeLog.Updated(_logger, binRangeId, prefix, _currentUser.Name);

        return await SucceededAsync(BinRangeMutationStatus.Updated, binRangeId, cancellationToken);
    }

    public async Task<BinRangeMutationResult> DeleteAsync(
        int binRangeId, CancellationToken cancellationToken = default)
    {
        var range = await _ranges.GetForUpdateAsync(binRangeId, cancellationToken);
        if (range is null) return NotFound(binRangeId);

        if (range.IsDeleted)
        {
            return Refused(BinRangeMutationResult.Failure(
                BinRangeMutationStatus.AlreadyInThatState,
                $"BIN range '{range.Prefix}' is already deleted."), range.Prefix);
        }

        var now = DateTime.UtcNow;
        var before = await SnapshotAsync(binRangeId, cancellationToken);

        // Soft delete: the row stops matching classification and leaves the default
        // listing, but nothing is erased and the prefix stays reserved.
        range.IsDeleted = true;
        range.DeletedAt = now;
        range.DeletedBy = _currentUser.Name;
        range.UpdatedAt = now;
        range.UpdatedBy = _currentUser.Name;

        _audit.Record(AuditAction.Deleted, AuditEntityTypes.BinRange, binRangeId,
            before, before with { IsDeleted = true });

        await _ranges.SaveChangesAsync(cancellationToken);

        BinRangeLog.Deleted(_logger, binRangeId, range.Prefix, _currentUser.Name);

        return await SucceededAsync(BinRangeMutationStatus.Deleted, binRangeId, cancellationToken);
    }

    public async Task<BinRangeMutationResult> RestoreAsync(
        int binRangeId, CancellationToken cancellationToken = default)
    {
        var range = await _ranges.GetForUpdateAsync(binRangeId, cancellationToken);
        if (range is null) return NotFound(binRangeId);

        if (!range.IsDeleted)
        {
            return Refused(BinRangeMutationResult.Failure(
                BinRangeMutationStatus.AlreadyInThatState,
                $"BIN range '{range.Prefix}' is not deleted."), range.Prefix);
        }

        var before = await SnapshotAsync(binRangeId, cancellationToken);

        BinRangeMapper.Undelete(range);
        range.UpdatedAt = DateTime.UtcNow;
        range.UpdatedBy = _currentUser.Name;

        _audit.Record(AuditAction.Updated, AuditEntityTypes.BinRange, binRangeId,
            before, before with { IsDeleted = false });

        await _ranges.SaveChangesAsync(cancellationToken);

        BinRangeLog.Restored(_logger, binRangeId, range.Prefix, _currentUser.Name);

        return await SucceededAsync(BinRangeMutationStatus.Restored, binRangeId, cancellationToken);
    }

    // What a create and an update both have to establish before they may proceed: the input
    // is well formed and its four names resolve to live rows.
    //
    // The scheme-mismatch check deliberately does NOT belong here. It runs after the
    // prefix-in-use check, because a duplicate prefix is the more specific refusal: telling
    // someone their scheme looks wrong on a row they were never going to be allowed to add
    // sends them off fixing the wrong thing.
    private async Task<(ResolvedReferences? Resolved, BinRangeMutationResult? Refused)> PrepareAsync(
        BinRangeInput input, CancellationToken cancellationToken)
    {
        if (!BinRangeValidator.TryValidate(input, out var error))
        {
            return (null, Refused(BinRangeMutationResult.Failure(
                BinRangeMutationStatus.Invalid, error), input.Prefix));
        }

        var (resolved, unresolved) = await ResolveAsync(input, cancellationToken);
        if (unresolved is not null) return (null, unresolved);

        return (resolved, null);
    }

    private async Task<(ResolvedReferences? Resolved, BinRangeMutationResult? Failure)> ResolveAsync(
        BinRangeInput input, CancellationToken cancellationToken)
    {
        var names = await _ranges.ResolveByNameAsync(
            input.CardScheme.Trim(), input.ProductType.Trim(),
            input.FundingType.Trim(), input.CountryCode.Trim(), cancellationToken);

        var missing = new List<string>();
        if (names.CardScheme is null) missing.Add($"CardScheme '{input.CardScheme}' does not exist.");
        if (names.ProductType is null) missing.Add($"ProductType '{input.ProductType}' does not exist.");
        if (names.FundingType is null) missing.Add($"FundingType '{input.FundingType}' does not exist.");
        if (names.Country is null) missing.Add($"CountryCode '{input.CountryCode}' does not exist.");

        if (missing.Count > 0)
        {
            // All of them at once: fixing one name only to be told about the next is a
            // poor way to fill in a form.
            return (null, Refused(BinRangeMutationResult.Failure(
                BinRangeMutationStatus.Invalid, string.Join(" ", missing)), input.Prefix));
        }

        return (new ResolvedReferences(
            names.CardScheme!.Value, names.ProductType!.Value,
            names.FundingType!.Value, names.Country!.Value), null);
    }

    /// <summary>
    /// Cross-checks the declared card scheme against what the prefix's digits say the
    /// network actually is, and refuses the write when they disagree - unless the caller
    /// ticks <see cref="BinRangeInput.AcknowledgeSchemeMismatch"/>. The resolved name is
    /// used rather than the raw input so the message matches how the row would be stored
    /// (canonical spelling, not "visa").
    /// </summary>
    private BinRangeMutationResult? SchemeMismatch(
        BinRangeInput input, ResolvedReferences resolved)
    {
        if (input.AcknowledgeSchemeMismatch) return null;

        var prefix = input.NormalizedPrefix();
        var declaredName = resolved.CardScheme.Name;

        var detected = _schemeDetector.Detect(prefix);
        if (_schemeDetector.Matches(detected, declaredName)) return null;

        var detectedName = _schemeDetector.DisplayName(detected);

        var message = detectedName is null
            ? $"Prefix {prefix} does not match any known card-scheme range, but the form declares {declaredName}. " +
              "Tick 'Save anyway' if this is intentional."
            : $"Prefix {prefix} is a {detectedName} range, but the form declares {declaredName}. " +
              "Tick 'Save anyway' if this is intentional.";

        BinRangeLog.SchemeMismatchRefused(
            _logger, prefix, declaredName, detectedName ?? "(none)", _currentUser.Name);

        return BinRangeMutationResult.Failure(BinRangeMutationStatus.SchemeMismatch, message);
    }

    private async Task<BinRangeMutationResult> ReviveAsync(
        BinRange existing, BinRangeInput input, ResolvedReferences resolved,
        CancellationToken cancellationToken)
    {
        var before = await SnapshotAsync(existing.BinRangeId, cancellationToken);

        BinRangeMapper.Apply(existing, input, resolved, _currentUser.Name, DateTime.UtcNow);
        BinRangeMapper.Undelete(existing);

        // Reviving is a change to a row that already exists, so it is audited as one.
        _audit.Record(AuditAction.Updated, AuditEntityTypes.BinRange, existing.BinRangeId,
            before, BinRangeMapper.ToSnapshot(input, resolved, isDeleted: false));

        await _ranges.SaveChangesAsync(cancellationToken);

        BinRangeLog.Revived(
            _logger, existing.BinRangeId, existing.Prefix, _currentUser.Name);

        return await SucceededAsync(
            BinRangeMutationStatus.Restored, existing.BinRangeId, cancellationToken);
    }

    private async Task<BinRangeSnapshot> SnapshotAsync(
        int binRangeId, CancellationToken cancellationToken)
    {
        var range = await _ranges.GetAsync(binRangeId, DateTime.UtcNow.Date, cancellationToken);

        return BinRangeSnapshot.From(range!);
    }

    private async Task<BinRangeMutationResult> SucceededAsync(
        BinRangeMutationStatus status, int binRangeId, CancellationToken cancellationToken)
    {
        // Re-read through the shared projection, so the caller gets the row exactly as the
        // browse listing would show it - names resolved, status derived.
        var range = await _ranges.GetAsync(binRangeId, DateTime.UtcNow.Date, cancellationToken);

        return BinRangeMutationResult.Success(status, range!);
    }

    private BinRangeMutationResult Refused(BinRangeMutationResult result, string prefix)
    {
        BinRangeLog.WriteRefused(
            _logger, prefix, result.Status.ToString(), result.Error ?? string.Empty);

        return result;
    }

    private BinRangeMutationResult NotFound(int binRangeId) =>
        Refused(BinRangeMutationResult.Failure(
            BinRangeMutationStatus.NotFound, $"No BIN range with id {binRangeId}."), "(unknown)");
}
