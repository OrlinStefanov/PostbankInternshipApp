using BinTool.Domain.Common;
using BinTool.Infrastructure.Data;

namespace BinTool.Infrastructure.Repositories;

// Reads the canonical names behind reference ids. Only live rows are considered, so soft-deleted
// reference data cannot be assigned to a new rule.
public class ReferenceDataRepository : IReferenceDataRepository
{
    private readonly AppDbContext _db;

    public ReferenceDataRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task<ReferenceNames> ResolveAsync(
        int currencyId, RuleCriteriaKey key, CancellationToken cancellationToken = default)
    {
        // One query per table, and only for the ids that were actually supplied - a wildcard
        // is not a lookup. They run in sequence because a DbContext serves one query at a
        // time; a key with every field wildcarded costs a single round trip.
        var currency = await _db.Currencies.AsNoTracking()
            .Where(x => !x.IsDeleted && x.CurrencyId == currencyId)
            .Select(x => x.Code)
            .FirstOrDefaultAsync(cancellationToken);

        string? scheme = null, product = null, funding = null, region = null;

        if (key.CardSchemeId is { } schemeId)
        {
            scheme = await _db.CardSchemes.AsNoTracking()
                .Where(x => !x.IsDeleted && x.CardSchemeId == schemeId)
                .Select(x => x.Name)
                .FirstOrDefaultAsync(cancellationToken);
        }

        if (key.ProductTypeId is { } productId)
        {
            product = await _db.ProductTypes.AsNoTracking()
                .Where(x => !x.IsDeleted && x.ProductTypeId == productId)
                .Select(x => x.Name)
                .FirstOrDefaultAsync(cancellationToken);
        }

        if (key.FundingTypeId is { } fundingId)
        {
            funding = await _db.FundingTypes.AsNoTracking()
                .Where(x => !x.IsDeleted && x.FundingTypeId == fundingId)
                .Select(x => x.Name)
                .FirstOrDefaultAsync(cancellationToken);
        }

        if (key.RegionId is { } regionId)
        {
            region = await _db.Regions.AsNoTracking()
                .Where(x => !x.IsDeleted && x.RegionId == regionId)
                .Select(x => x.Name)
                .FirstOrDefaultAsync(cancellationToken);
        }

        return new ReferenceNames(currency, scheme, product, funding, region);
    }
}
