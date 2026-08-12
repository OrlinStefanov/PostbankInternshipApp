using System.Linq.Expressions;
using BinTool.Application.Models.BinRanges;
using BinTool.Domain.Entities;

namespace BinTool.Infrastructure.Repositories;

// Shared between browsing and the single-range writes so the two cannot drift - most of all the
// status, which is derived here rather than stored. Being an expression, it translates to SQL
// instead of pulling rows into memory first.
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
