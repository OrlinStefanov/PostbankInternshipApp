using BinTool.Application.Models.BinRanges;
using BinTool.Infrastructure.Data;

namespace BinTool.Infrastructure.Repositories;

public class BinRangeRepository : IBinRangeRepository
{
    private readonly AppDbContext _db;

    public BinRangeRepository(AppDbContext db)
    {
        _db = db;
    }

    public Task<BinRangeListItem?> GetAsync(
        int binRangeId, DateTime today, CancellationToken cancellationToken = default) =>
        _db.BinRanges.AsNoTracking()
            .Where(b => b.BinRangeId == binRangeId)
            .Select(BinRangeProjection.ToListItem(today))
            .FirstOrDefaultAsync(cancellationToken)!;

    public async Task<PagedResult<BinRangeListItem>> SearchAsync(
        BinRangeQuery query, int page, int pageSize, DateTime today,
        CancellationToken cancellationToken = default)
    {
        var rows = ApplyFilters(_db.BinRanges.AsNoTracking(), query, today);

        // Counted before paging so the pager knows how many pages exist.
        var totalCount = await rows.CountAsync(cancellationToken);

        var items = await rows
            .OrderBy(b => b.Prefix)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(BinRangeProjection.ToListItem(today))
            .ToListAsync(cancellationToken);

        return new PagedResult<BinRangeListItem>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    public async Task<IReadOnlyList<BinRangeListItem>> GetManyAsync(
        IReadOnlyCollection<int> binRangeIds, DateTime today,
        CancellationToken cancellationToken = default)
    {
        var ids = binRangeIds.ToList();

        return await _db.BinRanges.AsNoTracking()
            .Where(b => ids.Contains(b.BinRangeId))
            .Select(BinRangeProjection.ToListItem(today))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<PrefixScheme>> ListLivePrefixSchemesAsync(
        CancellationToken cancellationToken = default) =>
        await _db.BinRanges.AsNoTracking()
            .Where(b => !b.IsDeleted)
            .Select(b => new PrefixScheme(b.BinRangeId, b.Prefix, b.CardScheme!.Name))
            .ToListAsync(cancellationToken);

    public async Task<BinRangeFilterOptions> GetFilterOptionsAsync(
        CancellationToken cancellationToken = default) => new()
    {
        CardSchemes = await _db.CardSchemes.AsNoTracking()
            .Where(x => !x.IsDeleted).OrderBy(x => x.Name)
            .Select(x => x.Name).ToListAsync(cancellationToken),

        ProductTypes = await _db.ProductTypes.AsNoTracking()
            .Where(x => !x.IsDeleted).OrderBy(x => x.Name)
            .Select(x => x.Name).ToListAsync(cancellationToken),

        FundingTypes = await _db.FundingTypes.AsNoTracking()
            .Where(x => !x.IsDeleted).OrderBy(x => x.Name)
            .Select(x => x.Name).ToListAsync(cancellationToken),

        Countries = await _db.Countries.AsNoTracking()
            .Where(x => !x.IsDeleted).OrderBy(x => x.Name)
            .Select(x => new CountryOption { IsoCode = x.IsoCode, Name = x.Name })
            .ToListAsync(cancellationToken),

        // Every account that has added a range, deleted rows included, so the "added by"
        // filter can reach a range no matter its current state.
        Creators = await _db.BinRanges.AsNoTracking()
            .Where(b => b.CreatedBy != null)
            .Select(b => b.CreatedBy!)
            .Distinct().OrderBy(name => name)
            .ToListAsync(cancellationToken)
    };

    public Task<BinRange?> GetForUpdateAsync(
        int binRangeId, CancellationToken cancellationToken = default) =>
        _db.BinRanges.FirstOrDefaultAsync(b => b.BinRangeId == binRangeId, cancellationToken);

    public Task<BinRange?> FindByPrefixAsync(
        string prefix, CancellationToken cancellationToken = default) =>
        _db.BinRanges.FirstOrDefaultAsync(b => b.Prefix == prefix, cancellationToken);

    public Task<bool> PrefixBelongsToAnotherAsync(
        string prefix, int binRangeId, CancellationToken cancellationToken = default) =>
        _db.BinRanges.AnyAsync(
            b => b.Prefix == prefix && b.BinRangeId != binRangeId, cancellationToken);

    public async Task<BinReferenceNames> ResolveByNameAsync(
        string cardScheme, string productType, string fundingType, string countryCode,
        CancellationToken cancellationToken = default)
    {
        // Matched lower-cased because SQLite compares text case-sensitively by default and
        // "visa" should find Visa. What comes back is the stored spelling, so an audit
        // entry never shows a case difference as a change.
        var scheme = cardScheme.ToLowerInvariant();
        var product = productType.ToLowerInvariant();
        var funding = fundingType.ToLowerInvariant();
        var country = countryCode.ToLowerInvariant();

        return new BinReferenceNames(
            await _db.CardSchemes.AsNoTracking()
                .Where(x => !x.IsDeleted && x.Name.ToLower() == scheme)
                .Select(x => (NamedReference?)new NamedReference(x.CardSchemeId, x.Name))
                .FirstOrDefaultAsync(cancellationToken),

            await _db.ProductTypes.AsNoTracking()
                .Where(x => !x.IsDeleted && x.Name.ToLower() == product)
                .Select(x => (NamedReference?)new NamedReference(x.ProductTypeId, x.Name))
                .FirstOrDefaultAsync(cancellationToken),

            await _db.FundingTypes.AsNoTracking()
                .Where(x => !x.IsDeleted && x.Name.ToLower() == funding)
                .Select(x => (NamedReference?)new NamedReference(x.FundingTypeId, x.Name))
                .FirstOrDefaultAsync(cancellationToken),

            await _db.Countries.AsNoTracking()
                .Where(x => !x.IsDeleted && x.IsoCode.ToLower() == country)
                .Select(x => (NamedReference?)new NamedReference(x.CountryId, x.IsoCode))
                .FirstOrDefaultAsync(cancellationToken));
    }

    public void Add(BinRange range) => _db.BinRanges.Add(range);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _db.SaveChangesAsync(cancellationToken);

    public async Task<ITransaction> BeginTransactionAsync(CancellationToken cancellationToken = default) =>
        new EfTransaction(await _db.Database.BeginTransactionAsync(cancellationToken));

    // Narrows by whichever filters were supplied. Status is a where clause per case rather than a
    // filter over the projection, so paging and the total both run in the database.
    private static IQueryable<BinRange> ApplyFilters(
        IQueryable<BinRange> rows, BinRangeQuery query, DateTime today)
    {
        var prefix = query.Prefix?.Trim();
        if (!string.IsNullOrEmpty(prefix))
        {
            rows = rows.Where(b => b.Prefix.StartsWith(prefix));
        }

        var cardScheme = Normalize(query.CardScheme);
        if (cardScheme is not null)
        {
            rows = rows.Where(b => b.CardScheme!.Name.ToLower() == cardScheme);
        }

        var productType = Normalize(query.ProductType);
        if (productType is not null)
        {
            rows = rows.Where(b => b.ProductType!.Name.ToLower() == productType);
        }

        var fundingType = Normalize(query.FundingType);
        if (fundingType is not null)
        {
            rows = rows.Where(b => b.FundingType!.Name.ToLower() == fundingType);
        }

        var countryCode = Normalize(query.CountryCode);
        if (countryCode is not null)
        {
            rows = rows.Where(b => b.Country!.IsoCode.ToLower() == countryCode);
        }

        var createdBy = Normalize(query.CreatedBy);
        if (createdBy is not null)
        {
            rows = rows.Where(b => b.CreatedBy != null && b.CreatedBy.ToLower() == createdBy);
        }

        return query.Status switch
        {
            BinRangeStatus.Deleted => rows.Where(b => b.IsDeleted),

            BinRangeStatus.Expired => rows.Where(b =>
                !b.IsDeleted && b.ValidTo != null && b.ValidTo < today),

            BinRangeStatus.Scheduled => rows.Where(b =>
                !b.IsDeleted && b.ValidFrom > today),

            BinRangeStatus.Active => rows.Where(b =>
                !b.IsDeleted && b.ValidFrom <= today && (b.ValidTo == null || b.ValidTo >= today)),

            // No status filter: show everything that still exists.
            _ => rows.Where(b => !b.IsDeleted)
        };
    }

    private static string? Normalize(string? value)
    {
        var trimmed = value?.Trim();

        return string.IsNullOrEmpty(trimmed) ? null : trimmed.ToLowerInvariant();
    }
}
