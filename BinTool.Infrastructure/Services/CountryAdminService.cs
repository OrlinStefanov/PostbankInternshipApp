using System.ComponentModel.DataAnnotations;
using BinTool.Core.Entities;
using BinTool.Core.Models.Audit;
using BinTool.Core.Models.ReferenceData;
using BinTool.Core.Services;
using BinTool.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BinTool.Infrastructure.Services;

public class CountryAdminService : ICountryAdminService
{
    private readonly AppDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IAuditLog _audit;

    public CountryAdminService(AppDbContext db, ICurrentUser currentUser, IAuditLog audit)
    {
        _db = db;
        _currentUser = currentUser;
        _audit = audit;
    }

    public async Task<List<CountryListItem>> SearchAsync(
        bool includeDeleted, CancellationToken cancellationToken = default)
    {
        var query = _db.Countries.AsNoTracking().Include(c => c.Region).AsQueryable();

        if (!includeDeleted)
        {
            query = query.Where(c => !c.IsDeleted);
        }

        return await query
            .OrderBy(c => c.IsoCode)
            .Select(c => Project(c))
            .ToListAsync(cancellationToken);
    }

    public async Task<CountryListItem?> GetAsync(int id, CancellationToken cancellationToken = default) =>
        await _db.Countries.AsNoTracking()
            .Include(c => c.Region)
            .Where(c => c.CountryId == id)
            .Select(c => Project(c))
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<CountryMutationResult> CreateAsync(
        CountryInput input, CancellationToken cancellationToken = default)
    {
        if (!TryValidate(input, out var invalid)) return invalid;

        var isoCode = input.IsoCode.Trim().ToUpperInvariant();
        var name = input.Name.Trim();

        var region = await ResolveRegionAsync(input.RegionId, cancellationToken);
        if (region is null)
        {
            return CountryMutationResult.Failure(
                LookupMutationStatus.Invalid,
                $"Region {input.RegionId} does not exist or is deleted.");
        }

        // The unique index on IsoCode spans soft-deleted rows too. Revive an existing
        // deleted row in place rather than duplicating - matches the BinRange pattern.
        var existing = await _db.Countries.FirstOrDefaultAsync(
            c => c.IsoCode == isoCode, cancellationToken);

        var now = DateTime.UtcNow;

        if (existing is { IsDeleted: false })
        {
            return CountryMutationResult.Failure(
                LookupMutationStatus.NameInUse,
                $"Country '{isoCode}' already exists.");
        }

        if (existing is { IsDeleted: true })
        {
            var before = new CountrySnapshot(
                existing.IsoCode, existing.Name,
                await RegionNameAsync(existing.RegionId, cancellationToken),
                IsDeleted: true);

            existing.Name = name;
            existing.RegionId = region.Value.Id;
            existing.IsDeleted = false;
            existing.DeletedAt = null;
            existing.DeletedBy = null;
            existing.UpdatedAt = now;
            existing.UpdatedBy = _currentUser.Name;

            _audit.Record(AuditAction.Updated, AuditEntityTypes.Country, existing.CountryId,
                before, new CountrySnapshot(isoCode, name, region.Value.Name, IsDeleted: false));

            await _db.SaveChangesAsync(cancellationToken);

            return await SucceededAsync(LookupMutationStatus.Restored, existing.CountryId, cancellationToken);
        }

        var country = new Country
        {
            IsoCode = isoCode,
            Name = name,
            RegionId = region.Value.Id,
            CreatedAt = now,
            CreatedBy = _currentUser.Name,
            UpdatedAt = now,
            UpdatedBy = _currentUser.Name
        };

        await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);

        _db.Countries.Add(country);
        await _db.SaveChangesAsync(cancellationToken);

        _audit.Record(AuditAction.Created, AuditEntityTypes.Country, country.CountryId,
            null, new CountrySnapshot(isoCode, name, region.Value.Name, IsDeleted: false));

        await _db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return await SucceededAsync(LookupMutationStatus.Created, country.CountryId, cancellationToken);
    }

    public async Task<CountryMutationResult> UpdateAsync(
        int id, CountryInput input, CancellationToken cancellationToken = default)
    {
        if (!TryValidate(input, out var invalid)) return invalid;

        var country = await _db.Countries.FirstOrDefaultAsync(
            c => c.CountryId == id, cancellationToken);

        if (country is null) return NotFound(id);

        if (country.IsDeleted)
        {
            return CountryMutationResult.Failure(
                LookupMutationStatus.NotFound,
                $"Country {id} is deleted. Restore it before editing.");
        }

        var isoCode = input.IsoCode.Trim().ToUpperInvariant();
        var name = input.Name.Trim();

        var region = await ResolveRegionAsync(input.RegionId, cancellationToken);
        if (region is null)
        {
            return CountryMutationResult.Failure(
                LookupMutationStatus.Invalid,
                $"Region {input.RegionId} does not exist or is deleted.");
        }

        if (isoCode != country.IsoCode)
        {
            var taken = await _db.Countries.AnyAsync(
                c => c.IsoCode == isoCode && c.CountryId != id, cancellationToken);

            if (taken)
            {
                return CountryMutationResult.Failure(
                    LookupMutationStatus.NameInUse,
                    $"Country '{isoCode}' already exists.");
            }
        }

        var before = new CountrySnapshot(
            country.IsoCode, country.Name,
            await RegionNameAsync(country.RegionId, cancellationToken),
            IsDeleted: false);

        var now = DateTime.UtcNow;
        country.IsoCode = isoCode;
        country.Name = name;
        country.RegionId = region.Value.Id;
        country.UpdatedAt = now;
        country.UpdatedBy = _currentUser.Name;

        _audit.Record(AuditAction.Updated, AuditEntityTypes.Country, id,
            before, new CountrySnapshot(isoCode, name, region.Value.Name, IsDeleted: false));

        await _db.SaveChangesAsync(cancellationToken);

        return await SucceededAsync(LookupMutationStatus.Updated, id, cancellationToken);
    }

    public async Task<CountryMutationResult> DeleteAsync(
        int id, CancellationToken cancellationToken = default)
    {
        var country = await _db.Countries.FirstOrDefaultAsync(
            c => c.CountryId == id, cancellationToken);

        if (country is null) return NotFound(id);

        if (country.IsDeleted)
        {
            return CountryMutationResult.Failure(
                LookupMutationStatus.AlreadyInThatState,
                $"Country '{country.IsoCode}' is already deleted.");
        }

        var referencing = await _db.BinRanges.AsNoTracking()
            .CountAsync(b => !b.IsDeleted && b.CountryId == id, cancellationToken);

        if (referencing > 0)
        {
            return CountryMutationResult.Failure(
                LookupMutationStatus.InUse,
                $"Country '{country.IsoCode}' is still used by {referencing} BIN range(s). Remove the references first.");
        }

        var before = new CountrySnapshot(
            country.IsoCode, country.Name,
            await RegionNameAsync(country.RegionId, cancellationToken),
            IsDeleted: false);

        var now = DateTime.UtcNow;
        country.IsDeleted = true;
        country.DeletedAt = now;
        country.DeletedBy = _currentUser.Name;
        country.UpdatedAt = now;
        country.UpdatedBy = _currentUser.Name;

        _audit.Record(AuditAction.Deleted, AuditEntityTypes.Country, id,
            before, before with { IsDeleted = true });

        await _db.SaveChangesAsync(cancellationToken);

        return await SucceededAsync(LookupMutationStatus.Deleted, id, cancellationToken);
    }

    public async Task<CountryMutationResult> RestoreAsync(
        int id, CancellationToken cancellationToken = default)
    {
        var country = await _db.Countries.FirstOrDefaultAsync(
            c => c.CountryId == id, cancellationToken);

        if (country is null) return NotFound(id);

        if (!country.IsDeleted)
        {
            return CountryMutationResult.Failure(
                LookupMutationStatus.AlreadyInThatState,
                $"Country '{country.IsoCode}' is not deleted.");
        }

        var before = new CountrySnapshot(
            country.IsoCode, country.Name,
            await RegionNameAsync(country.RegionId, cancellationToken),
            IsDeleted: true);

        var now = DateTime.UtcNow;
        country.IsDeleted = false;
        country.DeletedAt = null;
        country.DeletedBy = null;
        country.UpdatedAt = now;
        country.UpdatedBy = _currentUser.Name;

        _audit.Record(AuditAction.Updated, AuditEntityTypes.Country, id,
            before, before with { IsDeleted = false });

        await _db.SaveChangesAsync(cancellationToken);

        return await SucceededAsync(LookupMutationStatus.Restored, id, cancellationToken);
    }

    // ---- Support ---------------------------------------------------------------

    private static CountryListItem Project(Country c) => new()
    {
        Id = c.CountryId,
        IsoCode = c.IsoCode,
        Name = c.Name,
        RegionId = c.RegionId,
        RegionName = c.Region != null ? c.Region.Name : string.Empty,
        Status = c.IsDeleted ? LookupStatus.Deleted : LookupStatus.Active,
        CreatedAt = c.CreatedAt,
        CreatedBy = c.CreatedBy,
        UpdatedAt = c.UpdatedAt,
        UpdatedBy = c.UpdatedBy,
        DeletedAt = c.DeletedAt,
        DeletedBy = c.DeletedBy
    };

    /// <summary>
    /// Loads a live region by id. Deleted regions are not offered - a country cannot be
    /// assigned to one, matching how reference data works in BinRangeAdminService.
    /// </summary>
    private async Task<(int Id, string Name)?> ResolveRegionAsync(
        int regionId, CancellationToken cancellationToken)
    {
        var region = await _db.Regions.AsNoTracking()
            .Where(r => !r.IsDeleted && r.RegionId == regionId)
            .Select(r => new { r.RegionId, r.Name })
            .FirstOrDefaultAsync(cancellationToken);

        return region is null ? null : (region.RegionId, region.Name);
    }

    private async Task<string> RegionNameAsync(int regionId, CancellationToken cancellationToken)
    {
        var name = await _db.Regions.AsNoTracking()
            .Where(r => r.RegionId == regionId)
            .Select(r => r.Name)
            .FirstOrDefaultAsync(cancellationToken);

        return name ?? string.Empty;
    }

    private async Task<CountryMutationResult> SucceededAsync(
        LookupMutationStatus status, int id, CancellationToken cancellationToken)
    {
        var country = await GetAsync(id, cancellationToken);

        return CountryMutationResult.Success(status, country!);
    }

    private static CountryMutationResult NotFound(int id) =>
        CountryMutationResult.Failure(
            LookupMutationStatus.NotFound, $"No country with id {id}.");

    private static bool TryValidate(CountryInput input, out CountryMutationResult failure)
    {
        var results = new List<ValidationResult>();

        if (Validator.TryValidateObject(
                input, new ValidationContext(input), results, validateAllProperties: true))
        {
            failure = null!;
            return true;
        }

        failure = CountryMutationResult.Failure(
            LookupMutationStatus.Invalid, string.Join(" ", results.Select(r => r.ErrorMessage)));

        return false;
    }
}
