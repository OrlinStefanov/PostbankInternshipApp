using System.Linq.Expressions;
using BinTool.Application.Models.BinRanges;
using BinTool.Domain.Entities;

namespace BinTool.Infrastructure.Repositories;

internal static class BinRangeProjection
{
    public static Expression<Func<BinRange, BinRangeListItem>> ToListItem(DateTime today) =>
        b => new BinRangeListItem
        {
            BinRangeId = b.BinRangeId,
            Prefix = b.Prefix,
            PrefixLength = b.PrefixLength,
            CardScheme = b.CardScheme!.Name,
            ProductType = b.ProductType!.Name,
            FundingType = b.FundingType!.Name,
            CountryCode = b.Country!.IsoCode,
            CountryName = b.Country.Name,
            Region = b.Country.Region!.Name,
            ValidFrom = b.ValidFrom,
            ValidTo = b.ValidTo,
            Status = b.IsDeleted
                ? BinRangeStatus.Deleted
                : b.ValidTo != null && b.ValidTo < today
                    ? BinRangeStatus.Expired
                    : b.ValidFrom > today
                        ? BinRangeStatus.Scheduled
                        : BinRangeStatus.Active,
            CreatedAt = b.CreatedAt,
            CreatedBy = b.CreatedBy,
            UpdatedAt = b.UpdatedAt,
            UpdatedBy = b.UpdatedBy
        };
}
