using System.Globalization;
using BinTool.Application.Models.BinRanges;

namespace BinTool.Application.Models.Audit;

public sealed record BinRangeSnapshot(
    string Prefix,
    string? CardScheme,
    string? ProductType,
    string? FundingType,
    string? CountryCode,
    string ValidFrom,
    string? ValidTo,
    bool IsDeleted)
{
    public const string DateFormat = "yyyy-MM-dd";

    /// <summary>Takes the snapshot from a range already resolved for display.</summary>
    public static BinRangeSnapshot From(BinRangeListItem range, bool? isDeleted = null) => new(
        range.Prefix,
        range.CardScheme,
        range.ProductType,
        range.FundingType,
        range.CountryCode,
        Date(range.ValidFrom),
        range.ValidTo is null ? null : Date(range.ValidTo.Value),
        isDeleted ?? range.Status == BinRangeStatus.Deleted);

    public static string Date(DateTime value) =>
        value.ToString(DateFormat, CultureInfo.InvariantCulture);
}
