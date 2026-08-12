using BinTool.Application.Models.ReferenceData;
using BinTool.Domain.Common;
using BinTool.Infrastructure.Data;

namespace BinTool.Infrastructure.Repositories;

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
