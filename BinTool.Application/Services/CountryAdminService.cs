using BinTool.Application.Abstractions;
using BinTool.Application.Mapping;
using BinTool.Application.Models.Audit;
using BinTool.Application.Models.ReferenceData;
using BinTool.Application.Validation;
using Microsoft.Extensions.Logging;

namespace BinTool.Application.Services;

public class CountryAdminService : ICountryAdminService
{
    private readonly ICountryRepository _countries;
    private readonly ICurrentUser _currentUser;
    private readonly IAuditLog _audit;
    private readonly ILogger<CountryAdminService> _logger;

    public CountryAdminService(
        ICountryRepository countries,
        ICurrentUser currentUser,
        IAuditLog audit,
        ILogger<CountryAdminService> logger)
    {
        _countries = countries;
        _currentUser = currentUser;
        _audit = audit;
        _logger = logger;
    }

    public async Task<List<CountryListItem>> SearchAsync(
        bool includeDeleted, CancellationToken cancellationToken = default)
    {
        var rows = await _countries.ListAsync(includeDeleted, cancellationToken);

        return rows.Select(CountryMapper.ToListItem).ToList();
    }

    public async Task<CountryListItem?> GetAsync(int id, CancellationToken cancellationToken = default)
    {
        var country = await _countries.GetWithRegionAsync(id, cancellationToken);

        return country is null ? null : CountryMapper.ToListItem(country);
    }

    public async Task<CountryMutationResult> CreateAsync(
        CountryInput input, CancellationToken cancellationToken = default)
    {
        var (region, refused) = await PrepareAsync(input, cancellationToken);
        if (refused is not null) return refused;

        var isoCode = input.NormalizedIsoCode();
        var existing = await _countries.FindByIsoCodeAsync(isoCode, cancellationToken);

        if (existing is { IsDeleted: false })
        {
            return Refused(CountryMutationResult.Failure(
                LookupMutationStatus.NameInUse, $"Country '{isoCode}' already exists."));
        }

        // The unique index on IsoCode spans soft-deleted rows
        if (existing is { IsDeleted: true })
        {
            return await ReviveAsync(existing, input, region!.Value, cancellationToken);
        }

        var now = DateTime.UtcNow;
        var country = new Country
        {
            CreatedAt = now,
            CreatedBy = _currentUser.Name,
            UpdatedAt = now,
            UpdatedBy = _currentUser.Name
        };

        CountryMapper.Apply(country, input, region!.Value);

        // An insert has no id until it is saved, and the audit entry has to carry one.
        await using var transaction = await _countries.BeginTransactionAsync(cancellationToken);

        _countries.Add(country);
        await _countries.SaveChangesAsync(cancellationToken);

        _audit.Record(AuditAction.Created, AuditEntityTypes.Country, country.CountryId,
            null, CountryMapper.ToSnapshot(input, region.Value, isDeleted: false));

        await _countries.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        CountryLog.Created(_logger, country.CountryId, isoCode, region.Value.Name, _currentUser.Name);

        return await SucceededAsync(LookupMutationStatus.Created, country.CountryId, cancellationToken);
    }

    public async Task<CountryMutationResult> UpdateAsync(
        int id, CountryInput input, CancellationToken cancellationToken = default)
    {
        var country = await _countries.GetForUpdateAsync(id, cancellationToken);
        if (country is null) return NotFound(id);

        if (country.IsDeleted)
        {
            return Refused(CountryMutationResult.Failure(
                LookupMutationStatus.NotFound,
                $"Country {id} is deleted. Restore it before editing."), id);
        }

        var (region, refused) = await PrepareAsync(input, cancellationToken);
        if (refused is not null) return refused;

        var isoCode = input.NormalizedIsoCode();

        if (isoCode != country.IsoCode)
        {
            var taken = await _countries.FindByIsoCodeAsync(isoCode, cancellationToken);
            if (taken is not null && taken.CountryId != id)
            {
                return Refused(CountryMutationResult.Failure(
                    LookupMutationStatus.NameInUse, $"Country '{isoCode}' already exists."), id);
            }
        }

        var before = await SnapshotAsync(country, cancellationToken);

        CountryMapper.Apply(country, input, region!.Value);
        Touch(country);

        _audit.Record(AuditAction.Updated, AuditEntityTypes.Country, id,
            before, CountryMapper.ToSnapshot(input, region.Value, isDeleted: false));

        await _countries.SaveChangesAsync(cancellationToken);

        CountryLog.Updated(_logger, id, isoCode, _currentUser.Name);

        return await SucceededAsync(LookupMutationStatus.Updated, id, cancellationToken);
    }

    public async Task<CountryMutationResult> DeleteAsync(
        int id, CancellationToken cancellationToken = default)
    {
        var country = await _countries.GetForUpdateAsync(id, cancellationToken);
        if (country is null) return NotFound(id);

        if (country.IsDeleted)
        {
            return Refused(CountryMutationResult.Failure(
                LookupMutationStatus.AlreadyInThatState,
                $"Country '{country.IsoCode}' is already deleted."), id);
        }

        var referencing = await _countries.CountBinRangesUsingAsync(id, cancellationToken);
        if (referencing > 0)
        {
            return Refused(CountryMutationResult.Failure(
                LookupMutationStatus.InUse,
                $"Country '{country.IsoCode}' is still used by {referencing} BIN range(s). " +
                "Remove the references first."), id);
        }

        var before = await SnapshotAsync(country, cancellationToken);

        country.IsDeleted = true;
        country.DeletedAt = DateTime.UtcNow;
        country.DeletedBy = _currentUser.Name;
        Touch(country);

        _audit.Record(AuditAction.Deleted, AuditEntityTypes.Country, id,
            before, before with { IsDeleted = true });

        await _countries.SaveChangesAsync(cancellationToken);

        CountryLog.Deleted(_logger, id, country.IsoCode, _currentUser.Name);

        return await SucceededAsync(LookupMutationStatus.Deleted, id, cancellationToken);
    }

    public async Task<CountryMutationResult> RestoreAsync(
        int id, CancellationToken cancellationToken = default)
    {
        var country = await _countries.GetForUpdateAsync(id, cancellationToken);
        if (country is null) return NotFound(id);

        if (!country.IsDeleted)
        {
            return Refused(CountryMutationResult.Failure(
                LookupMutationStatus.AlreadyInThatState,
                $"Country '{country.IsoCode}' is not deleted."), id);
        }

        var before = await SnapshotAsync(country, cancellationToken);

        country.IsDeleted = false;
        country.DeletedAt = null;
        country.DeletedBy = null;
        Touch(country);

        _audit.Record(AuditAction.Updated, AuditEntityTypes.Country, id,
            before, before with { IsDeleted = false });

        await _countries.SaveChangesAsync(cancellationToken);

        CountryLog.Restored(_logger, id, country.IsoCode, _currentUser.Name);

        return await SucceededAsync(LookupMutationStatus.Restored, id, cancellationToken);
    }

    private async Task<(RegionIdentity? Region, CountryMutationResult? Refused)> PrepareAsync(
        CountryInput input, CancellationToken cancellationToken)
    {
        if (!CountryValidator.TryValidate(input, out var error))
        {
            return (null, Refused(CountryMutationResult.Failure(
                LookupMutationStatus.Invalid, error)));
        }

        var region = await _countries.FindLiveRegionAsync(input.RegionId, cancellationToken);
        if (region is null)
        {
            return (null, Refused(CountryMutationResult.Failure(
                LookupMutationStatus.Invalid,
                $"Region {input.RegionId} does not exist or is deleted.")));
        }

        return (region, null);
    }

    private async Task<CountryMutationResult> ReviveAsync(
        Country existing, CountryInput input, RegionIdentity region,
        CancellationToken cancellationToken)
    {
        var before = await SnapshotAsync(existing, cancellationToken);

        CountryMapper.Apply(existing, input, region);
        existing.IsDeleted = false;
        existing.DeletedAt = null;
        existing.DeletedBy = null;
        Touch(existing);

        _audit.Record(AuditAction.Updated, AuditEntityTypes.Country, existing.CountryId,
            before, CountryMapper.ToSnapshot(input, region, isDeleted: false));

        await _countries.SaveChangesAsync(cancellationToken);

        CountryLog.Revived(_logger, existing.CountryId, existing.IsoCode, _currentUser.Name);

        return await SucceededAsync(
            LookupMutationStatus.Restored, existing.CountryId, cancellationToken);
    }

    private async Task<CountrySnapshot> SnapshotAsync(
        Country country, CancellationToken cancellationToken)
    {
        var regionName = await _countries.GetRegionNameAsync(country.RegionId, cancellationToken);

        return CountryMapper.ToSnapshot(country, regionName);
    }

    private void Touch(Country country)
    {
        country.UpdatedAt = DateTime.UtcNow;
        country.UpdatedBy = _currentUser.Name;
    }

    private async Task<CountryMutationResult> SucceededAsync(
        LookupMutationStatus status, int id, CancellationToken cancellationToken)
    {
        var country = await GetAsync(id, cancellationToken);

        return CountryMutationResult.Success(status, country!);
    }

    private CountryMutationResult Refused(CountryMutationResult result, int countryId = 0)
    {
        CountryLog.WriteRefused(
            _logger, countryId, result.Status.ToString(), result.Error ?? string.Empty);

        return result;
    }

    private CountryMutationResult NotFound(int id) =>
        Refused(CountryMutationResult.Failure(
            LookupMutationStatus.NotFound, $"No country with id {id}."), id);
}
