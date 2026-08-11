using BinTool.Infrastructure.Data;

namespace BinTool.Infrastructure.Repositories;

public class CountryRepository : ICountryRepository
{
    private readonly AppDbContext _db;

    public CountryRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<Country>> ListAsync(
        bool includeDeleted, CancellationToken cancellationToken = default)
    {
        var query = _db.Countries.AsNoTracking().Include(c => c.Region).AsQueryable();
        if (!includeDeleted) query = query.Where(c => !c.IsDeleted);

        return await query.OrderBy(c => c.IsoCode).ToListAsync(cancellationToken);
    }

    public Task<Country?> GetWithRegionAsync(int id, CancellationToken cancellationToken = default) =>
        _db.Countries.AsNoTracking()
            .Include(c => c.Region)
            .FirstOrDefaultAsync(c => c.CountryId == id, cancellationToken);

    public Task<Country?> GetForUpdateAsync(int id, CancellationToken cancellationToken = default) =>
        _db.Countries.FirstOrDefaultAsync(c => c.CountryId == id, cancellationToken);

    public Task<Country?> FindByIsoCodeAsync(string isoCode, CancellationToken cancellationToken = default)
    {
        var upper = isoCode.ToUpperInvariant();

        return _db.Countries.FirstOrDefaultAsync(c => c.IsoCode == upper, cancellationToken);
    }

    public async Task<RegionIdentity?> FindLiveRegionAsync(
        int regionId, CancellationToken cancellationToken = default)
    {
        var region = await _db.Regions.AsNoTracking()
            .Where(r => !r.IsDeleted && r.RegionId == regionId)
            .Select(r => new { r.RegionId, r.Name })
            .FirstOrDefaultAsync(cancellationToken);

        return region is null ? null : new RegionIdentity(region.RegionId, region.Name);
    }

    public async Task<string> GetRegionNameAsync(int regionId, CancellationToken cancellationToken = default)
    {
        var name = await _db.Regions.AsNoTracking()
            .Where(r => r.RegionId == regionId)
            .Select(r => r.Name)
            .FirstOrDefaultAsync(cancellationToken);

        return name ?? string.Empty;
    }

    public Task<int> CountBinRangesUsingAsync(int countryId, CancellationToken cancellationToken = default) =>
        _db.BinRanges.AsNoTracking()
            .CountAsync(b => !b.IsDeleted && b.CountryId == countryId, cancellationToken);

    public void Add(Country country) => _db.Countries.Add(country);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _db.SaveChangesAsync(cancellationToken);

    public async Task<ITransaction> BeginTransactionAsync(CancellationToken cancellationToken = default) =>
        new EfTransaction(await _db.Database.BeginTransactionAsync(cancellationToken));
}
