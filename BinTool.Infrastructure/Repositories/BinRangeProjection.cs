using System.Linq.Expressions;
using BinTool.Domain.Entities;
using BinTool.Application.Models.BinRanges;

namespace BinTool.Infrastructure.Repositories;

/// <summary>
/// The one place a stored <see cref="BinRange"/> becomes a <see cref="BinRangeListItem"/>.
/// <para>
/// Shared between browsing and the single-range writes so the two cannot drift - most of
/// all the status, which is derived here rather than stored and so has to be computed the
/// same way everywhere. Being an expression, it translates to SQL instead of pulling rows
/// into memory first.
/// </para>
/// </summary>
internal static class BinRangeProjection
{
    /// <param name="today">The date the status is derived against, in UTC.</param>
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
