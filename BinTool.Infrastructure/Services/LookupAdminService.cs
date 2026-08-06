using System.ComponentModel.DataAnnotations;
using BinTool.Core.Entities;
using BinTool.Core.Models.Audit;
using BinTool.Core.Models.ReferenceData;
using BinTool.Core.Services;
using BinTool.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BinTool.Infrastructure.Services;

/// <summary>
/// The four Name+Description reference tables share the same shape - id, name,
/// description, soft-delete triplet - so one service handles all four. The
/// <see cref="LookupKind"/> selects which <see cref="DbSet{TEntity}"/> to read and which
/// audit entity-type constant to record; every other rule is identical.
/// </summary>
public class LookupAdminService : ILookupAdminService
{
    private readonly AppDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IAuditLog _audit;

    public LookupAdminService(AppDbContext db, ICurrentUser currentUser, IAuditLog audit)
    {
        _db = db;
        _currentUser = currentUser;
        _audit = audit;
    }

    public async Task<List<LookupListItem>> SearchAsync(
        LookupKind kind, bool includeDeleted, CancellationToken cancellationToken = default)
    {
        // Filter on the entity itself and project only what's left, so EF Core sees a
        // plain column predicate rather than a filter over a projected record type.
        switch (kind)
        {
            case LookupKind.CardScheme:
                {
                    var q = _db.CardSchemes.AsNoTracking().AsQueryable();
                    if (!includeDeleted) q = q.Where(x => !x.IsDeleted);
                    var rows = await q.OrderBy(x => x.Name)
                        .Select(x => new Row(x.CardSchemeId, x.Name, x.Description, x.IsDeleted, x.DeletedAt, x.DeletedBy))
                        .ToListAsync(cancellationToken);
                    return rows.Select(ToListItem).ToList();
                }
            case LookupKind.ProductType:
                {
                    var q = _db.ProductTypes.AsNoTracking().AsQueryable();
                    if (!includeDeleted) q = q.Where(x => !x.IsDeleted);
                    var rows = await q.OrderBy(x => x.Name)
                        .Select(x => new Row(x.ProductTypeId, x.Name, x.Description, x.IsDeleted, x.DeletedAt, x.DeletedBy))
                        .ToListAsync(cancellationToken);
                    return rows.Select(ToListItem).ToList();
                }
            case LookupKind.FundingType:
                {
                    var q = _db.FundingTypes.AsNoTracking().AsQueryable();
                    if (!includeDeleted) q = q.Where(x => !x.IsDeleted);
                    var rows = await q.OrderBy(x => x.Name)
                        .Select(x => new Row(x.FundingTypeId, x.Name, x.Description, x.IsDeleted, x.DeletedAt, x.DeletedBy))
                        .ToListAsync(cancellationToken);
                    return rows.Select(ToListItem).ToList();
                }
            case LookupKind.Region:
                {
                    var q = _db.Regions.AsNoTracking().AsQueryable();
                    if (!includeDeleted) q = q.Where(x => !x.IsDeleted);
                    var rows = await q.OrderBy(x => x.Name)
                        .Select(x => new Row(x.RegionId, x.Name, x.Description, x.IsDeleted, x.DeletedAt, x.DeletedBy))
                        .ToListAsync(cancellationToken);
                    return rows.Select(ToListItem).ToList();
                }
            default:
                throw new ArgumentOutOfRangeException(nameof(kind));
        }
    }

    public async Task<LookupListItem?> GetAsync(
        LookupKind kind, int id, CancellationToken cancellationToken = default) =>
        kind switch
        {
            LookupKind.CardScheme => await GetOne(_db.CardSchemes,
                x => x.CardSchemeId == id,
                x => new Row(x.CardSchemeId, x.Name, x.Description, x.IsDeleted, x.DeletedAt, x.DeletedBy),
                cancellationToken),

            LookupKind.ProductType => await GetOne(_db.ProductTypes,
                x => x.ProductTypeId == id,
                x => new Row(x.ProductTypeId, x.Name, x.Description, x.IsDeleted, x.DeletedAt, x.DeletedBy),
                cancellationToken),

            LookupKind.FundingType => await GetOne(_db.FundingTypes,
                x => x.FundingTypeId == id,
                x => new Row(x.FundingTypeId, x.Name, x.Description, x.IsDeleted, x.DeletedAt, x.DeletedBy),
                cancellationToken),

            LookupKind.Region => await GetOne(_db.Regions,
                x => x.RegionId == id,
                x => new Row(x.RegionId, x.Name, x.Description, x.IsDeleted, x.DeletedAt, x.DeletedBy),
                cancellationToken),

            _ => throw new ArgumentOutOfRangeException(nameof(kind))
        };

    public async Task<LookupMutationResult> CreateAsync(
        LookupKind kind, LookupInput input, CancellationToken cancellationToken = default)
    {
        if (!TryValidate(input, out var invalid)) return invalid;

        var name = input.Name.Trim();
        var description = string.IsNullOrWhiteSpace(input.Description) ? null : input.Description.Trim();
        var entityType = EntityTypeFor(kind);

        // The unique index on Name spans soft-deleted rows too. Revive it in place rather
        // than duplicating - the row keeps its id, its foreign keys and its audit trail.
        var existing = await FindByNameAsync(kind, name, cancellationToken);

        if (existing is { IsDeleted: false })
        {
            return LookupMutationResult.Failure(
                LookupMutationStatus.NameInUse,
                $"{DisplayName(kind)} '{name}' already exists.");
        }

        if (existing is { IsDeleted: true })
        {
            var before = new LookupSnapshot(existing.Name, existing.Description, IsDeleted: true);

            await UpdateRowAsync(kind, existing.Id, name, description, revive: true, cancellationToken);

            _audit.Record(AuditAction.Updated, entityType, existing.Id,
                before, new LookupSnapshot(name, description, IsDeleted: false));

            await _db.SaveChangesAsync(cancellationToken);

            return await SucceededAsync(kind, LookupMutationStatus.Restored, existing.Id, cancellationToken);
        }

        // An insert has no id until it is saved, and the audit entry has to carry one.
        // Same transactional pattern the BIN range admin service uses.
        await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);

        var newId = await InsertRowAsync(kind, name, description, cancellationToken);

        _audit.Record(AuditAction.Created, entityType, newId,
            null, new LookupSnapshot(name, description, IsDeleted: false));

        await _db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return await SucceededAsync(kind, LookupMutationStatus.Created, newId, cancellationToken);
    }

    public async Task<LookupMutationResult> UpdateAsync(
        LookupKind kind, int id, LookupInput input, CancellationToken cancellationToken = default)
    {
        if (!TryValidate(input, out var invalid)) return invalid;

        var current = await FindByIdAsync(kind, id, cancellationToken);
        if (current is null) return NotFound(kind, id);

        if (current.IsDeleted)
        {
            // Editing a deleted row would quietly resurrect it as a side effect.
            return LookupMutationResult.Failure(
                LookupMutationStatus.NotFound,
                $"{DisplayName(kind)} {id} is deleted. Restore it before editing.");
        }

        var name = input.Name.Trim();
        var description = string.IsNullOrWhiteSpace(input.Description) ? null : input.Description.Trim();
        var entityType = EntityTypeFor(kind);

        if (!string.Equals(name, current.Name, StringComparison.OrdinalIgnoreCase))
        {
            var taken = await FindByNameAsync(kind, name, cancellationToken);
            if (taken is not null && taken.Id != id)
            {
                return LookupMutationResult.Failure(
                    LookupMutationStatus.NameInUse,
                    $"{DisplayName(kind)} '{name}' already exists.");
            }
        }

        var before = new LookupSnapshot(current.Name, current.Description, IsDeleted: false);

        await UpdateRowAsync(kind, id, name, description, revive: false, cancellationToken);

        _audit.Record(AuditAction.Updated, entityType, id,
            before, new LookupSnapshot(name, description, IsDeleted: false));

        await _db.SaveChangesAsync(cancellationToken);

        return await SucceededAsync(kind, LookupMutationStatus.Updated, id, cancellationToken);
    }

    public async Task<LookupMutationResult> DeleteAsync(
        LookupKind kind, int id, CancellationToken cancellationToken = default)
    {
        var current = await FindByIdAsync(kind, id, cancellationToken);
        if (current is null) return NotFound(kind, id);

        if (current.IsDeleted)
        {
            return LookupMutationResult.Failure(
                LookupMutationStatus.AlreadyInThatState,
                $"{DisplayName(kind)} '{current.Name}' is already deleted.");
        }

        // Deletion is refused while the row is still referenced by live data. Restore
        // stays unconditional - a row that came back was fine before it left.
        var inUse = await CountLiveReferencesAsync(kind, id, cancellationToken);
        if (inUse.Count > 0)
        {
            return LookupMutationResult.Failure(
                LookupMutationStatus.InUse,
                $"{DisplayName(kind)} '{current.Name}' is still used by {inUse.Count} {inUse.Description}. Remove the references first.");
        }

        var before = new LookupSnapshot(current.Name, current.Description, IsDeleted: false);

        await SoftDeleteAsync(kind, id, cancellationToken);

        _audit.Record(AuditAction.Deleted, EntityTypeFor(kind), id,
            before, before with { IsDeleted = true });

        await _db.SaveChangesAsync(cancellationToken);

        return await SucceededAsync(kind, LookupMutationStatus.Deleted, id, cancellationToken);
    }

    public async Task<LookupMutationResult> RestoreAsync(
        LookupKind kind, int id, CancellationToken cancellationToken = default)
    {
        var current = await FindByIdAsync(kind, id, cancellationToken);
        if (current is null) return NotFound(kind, id);

        if (!current.IsDeleted)
        {
            return LookupMutationResult.Failure(
                LookupMutationStatus.AlreadyInThatState,
                $"{DisplayName(kind)} '{current.Name}' is not deleted.");
        }

        var before = new LookupSnapshot(current.Name, current.Description, IsDeleted: true);

        await UndeleteAsync(kind, id, cancellationToken);

        _audit.Record(AuditAction.Updated, EntityTypeFor(kind), id,
            before, before with { IsDeleted = false });

        await _db.SaveChangesAsync(cancellationToken);

        return await SucceededAsync(kind, LookupMutationStatus.Restored, id, cancellationToken);
    }

    // ---- Dispatch helpers ------------------------------------------------------

    /// <summary>
    /// Projected row shape shared by all four kinds. Reads go through this so the caller
    /// never needs the entity class.
    /// </summary>
    private sealed record Row(int Id, string Name, string? Description, bool IsDeleted, DateTime? DeletedAt, string? DeletedBy);

    private static async Task<LookupListItem?> GetOne<TEntity>(
        DbSet<TEntity> set,
        System.Linq.Expressions.Expression<Func<TEntity, bool>> where,
        System.Linq.Expressions.Expression<Func<TEntity, Row>> project,
        CancellationToken cancellationToken)
        where TEntity : class
    {
        var row = await set.AsNoTracking().Where(where).Select(project)
            .FirstOrDefaultAsync(cancellationToken);

        return row is null ? null : ToListItem(row);
    }

    private static LookupListItem ToListItem(Row row) => new()
    {
        Id = row.Id,
        Name = row.Name,
        Description = row.Description,
        Status = row.IsDeleted ? LookupStatus.Deleted : LookupStatus.Active,
        DeletedAt = row.DeletedAt,
        DeletedBy = row.DeletedBy
    };

    private async Task<Row?> FindByNameAsync(
        LookupKind kind, string name, CancellationToken cancellationToken)
    {
        var lowered = name.ToLowerInvariant();

        return kind switch
        {
            LookupKind.CardScheme => await _db.CardSchemes.AsNoTracking()
                .Where(x => x.Name.ToLower() == lowered)
                .Select(x => new Row(x.CardSchemeId, x.Name, x.Description, x.IsDeleted, x.DeletedAt, x.DeletedBy))
                .FirstOrDefaultAsync(cancellationToken),

            LookupKind.ProductType => await _db.ProductTypes.AsNoTracking()
                .Where(x => x.Name.ToLower() == lowered)
                .Select(x => new Row(x.ProductTypeId, x.Name, x.Description, x.IsDeleted, x.DeletedAt, x.DeletedBy))
                .FirstOrDefaultAsync(cancellationToken),

            LookupKind.FundingType => await _db.FundingTypes.AsNoTracking()
                .Where(x => x.Name.ToLower() == lowered)
                .Select(x => new Row(x.FundingTypeId, x.Name, x.Description, x.IsDeleted, x.DeletedAt, x.DeletedBy))
                .FirstOrDefaultAsync(cancellationToken),

            LookupKind.Region => await _db.Regions.AsNoTracking()
                .Where(x => x.Name.ToLower() == lowered)
                .Select(x => new Row(x.RegionId, x.Name, x.Description, x.IsDeleted, x.DeletedAt, x.DeletedBy))
                .FirstOrDefaultAsync(cancellationToken),

            _ => throw new ArgumentOutOfRangeException(nameof(kind))
        };
    }

    private async Task<Row?> FindByIdAsync(
        LookupKind kind, int id, CancellationToken cancellationToken) =>
        kind switch
        {
            LookupKind.CardScheme => await _db.CardSchemes.AsNoTracking()
                .Where(x => x.CardSchemeId == id)
                .Select(x => new Row(x.CardSchemeId, x.Name, x.Description, x.IsDeleted, x.DeletedAt, x.DeletedBy))
                .FirstOrDefaultAsync(cancellationToken),

            LookupKind.ProductType => await _db.ProductTypes.AsNoTracking()
                .Where(x => x.ProductTypeId == id)
                .Select(x => new Row(x.ProductTypeId, x.Name, x.Description, x.IsDeleted, x.DeletedAt, x.DeletedBy))
                .FirstOrDefaultAsync(cancellationToken),

            LookupKind.FundingType => await _db.FundingTypes.AsNoTracking()
                .Where(x => x.FundingTypeId == id)
                .Select(x => new Row(x.FundingTypeId, x.Name, x.Description, x.IsDeleted, x.DeletedAt, x.DeletedBy))
                .FirstOrDefaultAsync(cancellationToken),

            LookupKind.Region => await _db.Regions.AsNoTracking()
                .Where(x => x.RegionId == id)
                .Select(x => new Row(x.RegionId, x.Name, x.Description, x.IsDeleted, x.DeletedAt, x.DeletedBy))
                .FirstOrDefaultAsync(cancellationToken),

            _ => throw new ArgumentOutOfRangeException(nameof(kind))
        };

    /// <summary>
    /// Adds a tracked row and saves so the assigned id can be read. Deferred to per-kind
    /// blocks so each entity's own key property is respected and no reflection is needed.
    /// </summary>
    private async Task<int> InsertRowAsync(
        LookupKind kind, string name, string? description, CancellationToken cancellationToken)
    {
        switch (kind)
        {
            case LookupKind.CardScheme:
                var scheme = new CardScheme { Name = name, Description = description };
                _db.CardSchemes.Add(scheme);
                await _db.SaveChangesAsync(cancellationToken);
                return scheme.CardSchemeId;

            case LookupKind.ProductType:
                var product = new ProductType { Name = name, Description = description };
                _db.ProductTypes.Add(product);
                await _db.SaveChangesAsync(cancellationToken);
                return product.ProductTypeId;

            case LookupKind.FundingType:
                var funding = new FundingType { Name = name, Description = description };
                _db.FundingTypes.Add(funding);
                await _db.SaveChangesAsync(cancellationToken);
                return funding.FundingTypeId;

            case LookupKind.Region:
                var region = new Region { Name = name, Description = description };
                _db.Regions.Add(region);
                await _db.SaveChangesAsync(cancellationToken);
                return region.RegionId;

            default:
                throw new ArgumentOutOfRangeException(nameof(kind));
        }
    }

    private async Task UpdateRowAsync(
        LookupKind kind, int id, string name, string? description, bool revive, CancellationToken cancellationToken)
    {
        switch (kind)
        {
            case LookupKind.CardScheme:
                {
                    var row = await _db.CardSchemes.FirstAsync(x => x.CardSchemeId == id, cancellationToken);
                    row.Name = name; row.Description = description;
                    if (revive) { row.IsDeleted = false; row.DeletedAt = null; row.DeletedBy = null; }
                    break;
                }
            case LookupKind.ProductType:
                {
                    var row = await _db.ProductTypes.FirstAsync(x => x.ProductTypeId == id, cancellationToken);
                    row.Name = name; row.Description = description;
                    if (revive) { row.IsDeleted = false; row.DeletedAt = null; row.DeletedBy = null; }
                    break;
                }
            case LookupKind.FundingType:
                {
                    var row = await _db.FundingTypes.FirstAsync(x => x.FundingTypeId == id, cancellationToken);
                    row.Name = name; row.Description = description;
                    if (revive) { row.IsDeleted = false; row.DeletedAt = null; row.DeletedBy = null; }
                    break;
                }
            case LookupKind.Region:
                {
                    var row = await _db.Regions.FirstAsync(x => x.RegionId == id, cancellationToken);
                    row.Name = name; row.Description = description;
                    if (revive) { row.IsDeleted = false; row.DeletedAt = null; row.DeletedBy = null; }
                    break;
                }
            default:
                throw new ArgumentOutOfRangeException(nameof(kind));
        }
    }

    private async Task SoftDeleteAsync(LookupKind kind, int id, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var who = _currentUser.Name;

        switch (kind)
        {
            case LookupKind.CardScheme:
                {
                    var row = await _db.CardSchemes.FirstAsync(x => x.CardSchemeId == id, cancellationToken);
                    row.IsDeleted = true; row.DeletedAt = now; row.DeletedBy = who;
                    break;
                }
            case LookupKind.ProductType:
                {
                    var row = await _db.ProductTypes.FirstAsync(x => x.ProductTypeId == id, cancellationToken);
                    row.IsDeleted = true; row.DeletedAt = now; row.DeletedBy = who;
                    break;
                }
            case LookupKind.FundingType:
                {
                    var row = await _db.FundingTypes.FirstAsync(x => x.FundingTypeId == id, cancellationToken);
                    row.IsDeleted = true; row.DeletedAt = now; row.DeletedBy = who;
                    break;
                }
            case LookupKind.Region:
                {
                    var row = await _db.Regions.FirstAsync(x => x.RegionId == id, cancellationToken);
                    row.IsDeleted = true; row.DeletedAt = now; row.DeletedBy = who;
                    break;
                }
            default:
                throw new ArgumentOutOfRangeException(nameof(kind));
        }
    }

    private async Task UndeleteAsync(LookupKind kind, int id, CancellationToken cancellationToken)
    {
        switch (kind)
        {
            case LookupKind.CardScheme:
                {
                    var row = await _db.CardSchemes.FirstAsync(x => x.CardSchemeId == id, cancellationToken);
                    row.IsDeleted = false; row.DeletedAt = null; row.DeletedBy = null;
                    break;
                }
            case LookupKind.ProductType:
                {
                    var row = await _db.ProductTypes.FirstAsync(x => x.ProductTypeId == id, cancellationToken);
                    row.IsDeleted = false; row.DeletedAt = null; row.DeletedBy = null;
                    break;
                }
            case LookupKind.FundingType:
                {
                    var row = await _db.FundingTypes.FirstAsync(x => x.FundingTypeId == id, cancellationToken);
                    row.IsDeleted = false; row.DeletedAt = null; row.DeletedBy = null;
                    break;
                }
            case LookupKind.Region:
                {
                    var row = await _db.Regions.FirstAsync(x => x.RegionId == id, cancellationToken);
                    row.IsDeleted = false; row.DeletedAt = null; row.DeletedBy = null;
                    break;
                }
            default:
                throw new ArgumentOutOfRangeException(nameof(kind));
        }
    }

    /// <summary>
    /// Counts live rows that would break if this reference row disappeared. Deleted BIN
    /// ranges are ignored - they no longer match classification, so they cannot pin down
    /// a reference row that also needs to go.
    /// </summary>
    private async Task<(int Count, string Description)> CountLiveReferencesAsync(
        LookupKind kind, int id, CancellationToken cancellationToken) =>
        kind switch
        {
            LookupKind.CardScheme => (
                await _db.BinRanges.AsNoTracking()
                    .CountAsync(b => !b.IsDeleted && b.CardSchemeId == id, cancellationToken),
                "BIN range(s)"),

            LookupKind.ProductType => (
                await _db.BinRanges.AsNoTracking()
                    .CountAsync(b => !b.IsDeleted && b.ProductTypeId == id, cancellationToken),
                "BIN range(s)"),

            LookupKind.FundingType => (
                await _db.BinRanges.AsNoTracking()
                    .CountAsync(b => !b.IsDeleted && b.FundingTypeId == id, cancellationToken),
                "BIN range(s)"),

            // A region is not attached to BIN ranges directly, but to countries; a country
            // is attached to BIN ranges. Sum both so a region cannot vanish out from under
            // an active country either.
            LookupKind.Region => (
                await _db.Countries.AsNoTracking()
                    .CountAsync(c => !c.IsDeleted && c.RegionId == id, cancellationToken),
                "country/countries"),

            _ => throw new ArgumentOutOfRangeException(nameof(kind))
        };

    // ---- Support ---------------------------------------------------------------

    private async Task<LookupMutationResult> SucceededAsync(
        LookupKind kind, LookupMutationStatus status, int id, CancellationToken cancellationToken)
    {
        var item = await GetAsync(kind, id, cancellationToken);

        return LookupMutationResult.Success(status, item!);
    }

    private static LookupMutationResult NotFound(LookupKind kind, int id) =>
        LookupMutationResult.Failure(
            LookupMutationStatus.NotFound, $"No {DisplayName(kind)} with id {id}.");

    private static bool TryValidate(LookupInput input, out LookupMutationResult failure)
    {
        var results = new List<ValidationResult>();

        if (Validator.TryValidateObject(
                input, new ValidationContext(input), results, validateAllProperties: true))
        {
            failure = null!;
            return true;
        }

        failure = LookupMutationResult.Failure(
            LookupMutationStatus.Invalid, string.Join(" ", results.Select(r => r.ErrorMessage)));

        return false;
    }

    private static string EntityTypeFor(LookupKind kind) => kind switch
    {
        LookupKind.CardScheme => AuditEntityTypes.CardScheme,
        LookupKind.ProductType => AuditEntityTypes.ProductType,
        LookupKind.FundingType => AuditEntityTypes.FundingType,
        LookupKind.Region => AuditEntityTypes.Region,
        _ => throw new ArgumentOutOfRangeException(nameof(kind))
    };

    private static string DisplayName(LookupKind kind) => kind switch
    {
        LookupKind.CardScheme => "Card scheme",
        LookupKind.ProductType => "Product type",
        LookupKind.FundingType => "Funding type",
        LookupKind.Region => "Region",
        _ => kind.ToString()
    };
}
