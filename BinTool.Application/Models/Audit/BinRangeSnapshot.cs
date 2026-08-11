using System.Globalization;
using BinTool.Application.Models.BinRanges;

namespace BinTool.Application.Models.Audit;

/// <summary>
/// What a BIN range looked like at one moment, as stored in an audit entry's
/// <c>OldValues</c> / <c>NewValues</c>.
/// <para>
/// Reference data is held by name, and dates as <c>yyyy-MM-dd</c> text, because an audit
/// trail is read by people. Lookup ids would be accurate and useless: they say nothing on
/// their own, and they stop meaning the same thing if the reference tables are ever
/// renumbered. Names are copied at the time of the change, so the entry keeps saying what
/// the range was even if the reference data is renamed afterwards.
/// </para>
/// </summary>
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
    /// <param name="range">The range to describe.</param>
    /// <param name="isDeleted">
    /// Overrides the deleted flag, for describing the far side of a delete or restore
    /// without re-reading the row.
    /// </param>
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
