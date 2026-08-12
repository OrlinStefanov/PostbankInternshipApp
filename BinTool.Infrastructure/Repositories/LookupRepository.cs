using System.Linq.Expressions;
using BinTool.Application.Models.ReferenceData;
using BinTool.Infrastructure.Data;

namespace BinTool.Infrastructure.Repositories;

public class LookupRepository : ILookupRepository
{
    private readonly AppDbContext _db;

    public LookupRepository(AppDbContext db)
    {
        _db = db;
    }

    public Task<IReadOnlyList<ILookupEntity>> ListAsync(
        LookupKind kind, bool includeDeleted, CancellationToken cancellationToken = default) =>
        kind switch
        {
            LookupKind.CardScheme => List(_db.CardSchemes, includeDeleted, cancellationToken),
            LookupKind.ProductType => List(_db.ProductTypes, includeDeleted, cancellationToken),
            LookupKind.FundingType => List(_db.FundingTypes, includeDeleted, cancellationToken),
            LookupKind.Region => List(_db.Regions, includeDeleted, cancellationToken),
            _ => throw Unknown(kind)
        };

    public Task<ILookupEntity?> GetAsync(
        LookupKind kind, int id, CancellationToken cancellationToken = default) =>
        kind switch
        {
            LookupKind.CardScheme => Find(_db.CardSchemes.AsNoTracking(), x => x.CardSchemeId == id, cancellationToken),
            LookupKind.ProductType => Find(_db.ProductTypes.AsNoTracking(), x => x.ProductTypeId == id, cancellationToken),
            LookupKind.FundingType => Find(_db.FundingTypes.AsNoTracking(), x => x.FundingTypeId == id, cancellationToken),
            LookupKind.Region => Find(_db.Regions.AsNoTracking(), x => x.RegionId == id, cancellationToken),
            _ => throw Unknown(kind)
        };

    public Task<ILookupEntity?> GetForUpdateAsync(
        LookupKind kind, int id, CancellationToken cancellationToken = default) =>
        kind switch
        {
            LookupKind.CardScheme => Find(_db.CardSchemes, x => x.CardSchemeId == id, cancellationToken),
            LookupKind.ProductType => Find(_db.ProductTypes, x => x.ProductTypeId == id, cancellationToken),
            LookupKind.FundingType => Find(_db.FundingTypes, x => x.FundingTypeId == id, cancellationToken),
            LookupKind.Region => Find(_db.Regions, x => x.RegionId == id, cancellationToken),
            _ => throw Unknown(kind)
        };

    public Task<ILookupEntity?> FindByNameAsync(
        LookupKind kind, string name, CancellationToken cancellationToken = default)
    {
        // Lowered on both sides rather than with StringComparison, which EF cannot translate.
        var lowered = name.ToLowerInvariant();

        return kind switch
        {
            LookupKind.CardScheme => Find(_db.CardSchemes, x => x.Name.ToLower() == lowered, cancellationToken),
            LookupKind.ProductType => Find(_db.ProductTypes, x => x.Name.ToLower() == lowered, cancellationToken),
            LookupKind.FundingType => Find(_db.FundingTypes, x => x.Name.ToLower() == lowered, cancellationToken),
            LookupKind.Region => Find(_db.Regions, x => x.Name.ToLower() == lowered, cancellationToken),
            _ => throw Unknown(kind)
        };
    }

    public ILookupEntity Add(LookupKind kind, string name, string? description) => kind switch
    {
        LookupKind.CardScheme => Stage(_db.CardSchemes, new CardScheme { Name = name, Description = description }),
        LookupKind.ProductType => Stage(_db.ProductTypes, new ProductType { Name = name, Description = description }),
        LookupKind.FundingType => Stage(_db.FundingTypes, new FundingType { Name = name, Description = description }),
        LookupKind.Region => Stage(_db.Regions, new Region { Name = name, Description = description }),
        _ => throw Unknown(kind)
    };

    public Task<int> CountLiveReferencesAsync(
        LookupKind kind, int id, CancellationToken cancellationToken = default) => kind switch
        {
            LookupKind.CardScheme => _db.BinRanges.AsNoTracking()
                .CountAsync(b => !b.IsDeleted && b.CardSchemeId == id, cancellationToken),

            LookupKind.ProductType => _db.BinRanges.AsNoTracking()
                .CountAsync(b => !b.IsDeleted && b.ProductTypeId == id, cancellationToken),

            LookupKind.FundingType => _db.BinRanges.AsNoTracking()
                .CountAsync(b => !b.IsDeleted && b.FundingTypeId == id, cancellationToken),

            // A region reaches BIN ranges through countries rather than directly, so what pins
            // it down is a live country, not a live range.
            LookupKind.Region => _db.Countries.AsNoTracking()
                .CountAsync(c => !c.IsDeleted && c.RegionId == id, cancellationToken),

            _ => throw Unknown(kind)
        };

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _db.SaveChangesAsync(cancellationToken);

    public async Task<ITransaction> BeginTransactionAsync(CancellationToken cancellationToken = default) =>
        new EfTransaction(await _db.Database.BeginTransactionAsync(cancellationToken));

    private static async Task<IReadOnlyList<ILookupEntity>> List<T>(
        DbSet<T> set, bool includeDeleted, CancellationToken cancellationToken)
        where T : class, ILookupEntity
    {
        var query = set.AsNoTracking().AsQueryable();
        if (!includeDeleted) query = query.Where(x => !x.IsDeleted);

        return await query.OrderBy(x => x.Name).ToListAsync(cancellationToken);
    }

    private static async Task<ILookupEntity?> Find<T>(
        IQueryable<T> query, Expression<Func<T, bool>> where, CancellationToken cancellationToken)
        where T : class, ILookupEntity =>
        await query.FirstOrDefaultAsync(where, cancellationToken);

    private static T Stage<T>(DbSet<T> set, T entity) where T : class, ILookupEntity
    {
        set.Add(entity);
        return entity;
    }

    private static ArgumentOutOfRangeException Unknown(LookupKind kind) =>
        new(nameof(kind), kind, "Unknown lookup kind.");
}
