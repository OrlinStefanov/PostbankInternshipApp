using System.ComponentModel.DataAnnotations;
using BinTool.Domain.Entities;
using BinTool.Application.Models.Audit;
using BinTool.Application.Models.BinRanges;
using BinTool.Application.Abstractions;
using BinTool.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BinTool.Infrastructure.Services;

public class BinRangeAdminService : IBinRangeAdminService
{
    private readonly AppDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IAuditLog _audit;
    private readonly ICardSchemeDetector _schemeDetector;

    public BinRangeAdminService(AppDbContext db, ICurrentUser currentUser, IAuditLog audit, ICardSchemeDetector schemeDetector)
    {
        _db = db;
        _currentUser = currentUser;
        _audit = audit;
        _schemeDetector = schemeDetector;
    }

    public async Task<BinRangeListItem?> GetAsync(
        int binRangeId, CancellationToken cancellationToken = default)
    {
        return await _db.BinRanges
            .AsNoTracking()
            .Where(b => b.BinRangeId == binRangeId)
            .Select(BinRangeProjection.ToListItem(DateTime.UtcNow.Date))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<BinRangeMutationResult> CreateAsync(
        BinRangeInput input, CancellationToken cancellationToken = default)
    {
        if (!TryValidate(input, out var invalid)) return invalid;

        var prefix = input.Prefix.Trim();

        var (resolved, unresolved) = await ResolveAsync(input, cancellationToken);

        if (unresolved is not null) return unresolved;

        // The unique index on Prefix spans soft-deleted rows, so a deleted range would
        // block the insert. Revive it with the new values instead - the same rule the CSV
        // import follows, and it keeps the row's id and audit trail.
        var existing = await _db.BinRanges
            .FirstOrDefaultAsync(b => b.Prefix == prefix, cancellationToken);

        if (existing is not null && !existing.IsDeleted)
        {
            return BinRangeMutationResult.Failure(
                BinRangeMutationStatus.PrefixInUse,
                $"Prefix '{prefix}' already belongs to another BIN range.");
        }

        // Held in reserve: an admin can accept the row anyway (co-brand block, new
        // allocation the detector does not know) but never by accident.
        if (SchemeMismatch(prefix, resolved, input) is { } mismatch) return mismatch;

        var now = DateTime.UtcNow;

        if (existing is not null)
        {
            var before = await SnapshotAsync(existing.BinRangeId, cancellationToken);

            Apply(existing, prefix, resolved, input, now);
            existing.IsDeleted = false;
            existing.DeletedAt = null;
            existing.DeletedBy = null;

            // Reviving is a change to a row that already exists, so it is audited as one.
            _audit.Record(AuditAction.Updated, AuditEntityTypes.BinRange, existing.BinRangeId,
                before, Snapshot(prefix, resolved, input, isDeleted: false));

            await _db.SaveChangesAsync(cancellationToken);

            return await SucceededAsync(
                BinRangeMutationStatus.Restored, existing.BinRangeId, cancellationToken);
        }

        var range = new BinRange { CreatedAt = now, CreatedBy = _currentUser.Name };
        Apply(range, prefix, resolved, input, now);

        // An insert has no id until it is saved, and the audit entry has to carry one.
        // The transaction is what keeps the pair atomic across the two saves - a range
        // that committed without its audit row would be a silent hole in the trail.
        await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);

        _db.BinRanges.Add(range);
        await _db.SaveChangesAsync(cancellationToken);

        _audit.Record(AuditAction.Created, AuditEntityTypes.BinRange, range.BinRangeId,
            null, Snapshot(prefix, resolved, input, isDeleted: false));

        await _db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return await SucceededAsync(
            BinRangeMutationStatus.Created, range.BinRangeId, cancellationToken);
    }

    public async Task<BinRangeMutationResult> UpdateAsync(
        int binRangeId, BinRangeInput input, CancellationToken cancellationToken = default)
    {
        if (!TryValidate(input, out var invalid)) return invalid;

        var range = await _db.BinRanges
            .FirstOrDefaultAsync(b => b.BinRangeId == binRangeId, cancellationToken);

        if (range is null) return NotFound(binRangeId);

        if (range.IsDeleted)
        {
            // Editing a deleted range would quietly resurrect it as a side effect.
            return BinRangeMutationResult.Failure(
                BinRangeMutationStatus.NotFound,
                $"BIN range {binRangeId} is deleted. Restore it before editing.");
        }

        var prefix = input.Prefix.Trim();

        var (resolved, unresolved) = await ResolveAsync(input, cancellationToken);
        if (unresolved is not null) return unresolved;

        if (prefix != range.Prefix)
        {
            var taken = await _db.BinRanges.AnyAsync(
                b => b.Prefix == prefix && b.BinRangeId != binRangeId, cancellationToken);

            if (taken)
            {
                return BinRangeMutationResult.Failure(
                    BinRangeMutationStatus.PrefixInUse,
                    $"Prefix '{prefix}' already belongs to another BIN range.");
            }
        }

        // Same rule as on insert: the network the digits belong to has to match the name
        // being saved, or the caller has to say they know and want it anyway.
        if (SchemeMismatch(prefix, resolved, input) is { } mismatch) return mismatch;

        // Read the stored values before they are overwritten - afterwards they are gone.
        var before = await SnapshotAsync(binRangeId, cancellationToken);

        Apply(range, prefix, resolved, input, DateTime.UtcNow);

        _audit.Record(AuditAction.Updated, AuditEntityTypes.BinRange, binRangeId,
            before, Snapshot(prefix, resolved, input, isDeleted: false));

        await _db.SaveChangesAsync(cancellationToken);

        return await SucceededAsync(
            BinRangeMutationStatus.Updated, binRangeId, cancellationToken);
    }

    public async Task<BinRangeMutationResult> DeleteAsync(
        int binRangeId, CancellationToken cancellationToken = default)
    {
        var range = await _db.BinRanges
            .FirstOrDefaultAsync(b => b.BinRangeId == binRangeId, cancellationToken);

        if (range is null) return NotFound(binRangeId);

        if (range.IsDeleted)
        {
            return BinRangeMutationResult.Failure(
                BinRangeMutationStatus.AlreadyInThatState,
                $"BIN range '{range.Prefix}' is already deleted.");
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

        // Nothing but the flag moved, so the two sides differ only there - which is
        // exactly what a reader needs to see.
        _audit.Record(AuditAction.Deleted, AuditEntityTypes.BinRange, binRangeId,
            before, before with { IsDeleted = true });

        await _db.SaveChangesAsync(cancellationToken);

        return await SucceededAsync(
            BinRangeMutationStatus.Deleted, binRangeId, cancellationToken);
    }

    public async Task<BinRangeMutationResult> RestoreAsync(
        int binRangeId, CancellationToken cancellationToken = default)
    {
        var range = await _db.BinRanges
            .FirstOrDefaultAsync(b => b.BinRangeId == binRangeId, cancellationToken);

        if (range is null) return NotFound(binRangeId);

        if (!range.IsDeleted)
        {
            return BinRangeMutationResult.Failure(
                BinRangeMutationStatus.AlreadyInThatState,
                $"BIN range '{range.Prefix}' is not deleted.");
        }

        var now = DateTime.UtcNow;
        var before = await SnapshotAsync(binRangeId, cancellationToken);

        range.IsDeleted = false;
        range.DeletedAt = null;
        range.DeletedBy = null;
        range.UpdatedAt = now;
        range.UpdatedBy = _currentUser.Name;

        _audit.Record(AuditAction.Updated, AuditEntityTypes.BinRange, binRangeId,
            before, before with { IsDeleted = false });

        await _db.SaveChangesAsync(cancellationToken);

        return await SucceededAsync(
            BinRangeMutationStatus.Restored, binRangeId, cancellationToken);
    }

    /// <summary>
    /// Writes the supplied values onto a range. Everything a caller can set goes through
    /// here, so no field is silently left behind when the input model grows.
    /// </summary>
    private void Apply(
        BinRange range, string prefix, Lookup resolved, BinRangeInput input, DateTime now)
    {
        range.Prefix = prefix;
        range.PrefixLength = prefix.Length;
        range.CardSchemeId = resolved.CardSchemeId;
        range.ProductTypeId = resolved.ProductTypeId;
        range.FundingTypeId = resolved.FundingTypeId;
        range.CountryId = resolved.CountryId;
        range.ValidFrom = input.ValidFrom.Date;
        range.ValidTo = input.ValidTo?.Date;
        range.UpdatedAt = now;
        range.UpdatedBy = _currentUser.Name;
    }

    /// <summary>
    /// Cross-checks the row's declared card scheme against what the prefix's digits say
    /// the network actually is, and refuses the write when they disagree - unless the
    /// caller ticks <see cref="BinRangeInput.AcknowledgeSchemeMismatch"/>. The resolved
    /// name is used rather than the raw input so the message matches how the row would be
    /// stored (canonical spelling, not "visa").
    /// </summary>
    private BinRangeMutationResult? SchemeMismatch(
        string prefix, Lookup resolved, BinRangeInput input)
    {
        if (input.AcknowledgeSchemeMismatch) return null;

        var detected = _schemeDetector.Detect(prefix);
        if (_schemeDetector.Matches(detected, resolved.CardScheme.Name)) return null;

        var detectedName = _schemeDetector.DisplayName(detected);
        var declaredName = resolved.CardScheme.Name;

        var message = detectedName is null
            ? $"Prefix {prefix} does not match any known card-scheme range, but the form declares {declaredName}. " +
              "Tick 'Save anyway' if this is intentional."
            : $"Prefix {prefix} is a {detectedName} range, but the form declares {declaredName}. " +
              "Tick 'Save anyway' if this is intentional.";

        return BinRangeMutationResult.Failure(BinRangeMutationStatus.SchemeMismatch, message);
    }

    /// <summary>
    /// Reads the stored row as an audit snapshot. Called before a change is applied, so
    /// the entry can say what the values were and not only what they became.
    /// </summary>
    private async Task<BinRangeSnapshot> SnapshotAsync(
        int binRangeId, CancellationToken cancellationToken)
    {
        var range = await _db.BinRanges
            .AsNoTracking()
            .Where(b => b.BinRangeId == binRangeId)
            .Select(BinRangeProjection.ToListItem(DateTime.UtcNow.Date))
            .FirstAsync(cancellationToken);

        return BinRangeSnapshot.From(range);
    }

    /// <summary>
    /// Builds the after snapshot from the values about to be written, using the canonical
    /// reference names resolving already produced - so it describes the row that is being
    /// saved without having to read it back first.
    /// </summary>
    private static BinRangeSnapshot Snapshot(
        string prefix, Lookup resolved, BinRangeInput input, bool isDeleted) =>
        new(prefix,
            resolved.CardScheme.Name,
            resolved.ProductType.Name,
            resolved.FundingType.Name,
            resolved.Country.Name,
            BinRangeSnapshot.Date(input.ValidFrom.Date),
            input.ValidTo is null ? null : BinRangeSnapshot.Date(input.ValidTo.Value.Date),
            isDeleted);

    /// <summary>
    /// Re-reads the saved row through the shared projection, so the caller gets the row
    /// exactly as the browse listing would show it - names resolved, status derived.
    /// </summary>
    private async Task<BinRangeMutationResult> SucceededAsync(
        BinRangeMutationStatus status, int binRangeId, CancellationToken cancellationToken)
    {
        var range = await _db.BinRanges
            .AsNoTracking()
            .Where(b => b.BinRangeId == binRangeId)
            .Select(BinRangeProjection.ToListItem(DateTime.UtcNow.Date))
            .FirstAsync(cancellationToken);

        return BinRangeMutationResult.Success(status, range);
    }

    private static BinRangeMutationResult NotFound(int binRangeId) =>
        BinRangeMutationResult.Failure(
            BinRangeMutationStatus.NotFound, $"No BIN range with id {binRangeId}.");

    /// <summary>
    /// Applies the model's own annotations. The API validates the body before the action
    /// runs, so this is only reached by a direct caller - but it keeps the rules in one
    /// place instead of trusting whoever called in.
    /// </summary>
    private static bool TryValidate(BinRangeInput input, out BinRangeMutationResult failure)
    {
        var results = new List<ValidationResult>();

        if (Validator.TryValidateObject(
                input, new ValidationContext(input), results, validateAllProperties: true))
        {
            failure = null!;
            return true;
        }

        failure = BinRangeMutationResult.Failure(
            BinRangeMutationStatus.Invalid,
            string.Join(" ", results.Select(r => r.ErrorMessage)));

        return false;
    }

    /// <summary>
    /// Turns the named reference data into ids, and hands back the canonical spelling of
    /// each name along with it. Names are matched lower-cased because SQLite compares text
    /// case-sensitively by default, and "visa" should find Visa - but what gets recorded
    /// afterwards is "Visa", so an audit entry never shows a case difference as a change.
    /// Soft-deleted reference data is not offered, so it cannot be assigned either.
    /// </summary>
    private async Task<(Lookup Resolved, BinRangeMutationResult? Failure)> ResolveAsync(
        BinRangeInput input, CancellationToken cancellationToken)
    {
        var cardScheme = input.CardScheme.Trim().ToLowerInvariant();
        var productType = input.ProductType.Trim().ToLowerInvariant();
        var fundingType = input.FundingType.Trim().ToLowerInvariant();
        var countryCode = input.CountryCode.Trim().ToLowerInvariant();

        var scheme = await _db.CardSchemes.AsNoTracking()
            .Where(x => !x.IsDeleted && x.Name.ToLower() == cardScheme)
            .Select(x => new Named(x.CardSchemeId, x.Name)).FirstOrDefaultAsync(cancellationToken);

        var product = await _db.ProductTypes.AsNoTracking()
            .Where(x => !x.IsDeleted && x.Name.ToLower() == productType)
            .Select(x => new Named(x.ProductTypeId, x.Name)).FirstOrDefaultAsync(cancellationToken);

        var funding = await _db.FundingTypes.AsNoTracking()
            .Where(x => !x.IsDeleted && x.Name.ToLower() == fundingType)
            .Select(x => new Named(x.FundingTypeId, x.Name)).FirstOrDefaultAsync(cancellationToken);

        var country = await _db.Countries.AsNoTracking()
            .Where(x => !x.IsDeleted && x.IsoCode.ToLower() == countryCode)
            .Select(x => new Named(x.CountryId, x.IsoCode)).FirstOrDefaultAsync(cancellationToken);

        var missing = new List<string>();
        if (scheme is null) missing.Add($"CardScheme '{input.CardScheme}' does not exist.");
        if (product is null) missing.Add($"ProductType '{input.ProductType}' does not exist.");
        if (funding is null) missing.Add($"FundingType '{input.FundingType}' does not exist.");
        if (country is null) missing.Add($"CountryCode '{input.CountryCode}' does not exist.");

        if (missing.Count > 0)
        {
            // All of them at once: fixing one name only to be told about the next is a
            // poor way to fill in a form.
            return (default, BinRangeMutationResult.Failure(
                BinRangeMutationStatus.Invalid, string.Join(" ", missing)));
        }

        return (new Lookup(scheme!, product!, funding!, country!), null);
    }

    private sealed record Named(int Id, string Name);

    private readonly record struct Lookup(
        Named CardScheme, Named ProductType, Named FundingType, Named Country)
    {
        public int CardSchemeId => CardScheme.Id;
        public int ProductTypeId => ProductType.Id;
        public int FundingTypeId => FundingType.Id;
        public int CountryId => Country.Id;
    }
}