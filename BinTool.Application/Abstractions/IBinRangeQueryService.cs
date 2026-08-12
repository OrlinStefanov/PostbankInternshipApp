using BinTool.Application.Models.BinRanges;

namespace BinTool.Application.Abstractions;

public interface IBinRangeQueryService
{
    /// <summary>
    /// Returns one page of BIN ranges matching the filters, ordered by prefix, with the
    /// lookup ids resolved to names and a status derived from the validity dates.
    /// </summary>
    Task<PagedResult<BinRangeListItem>> SearchAsync(
        BinRangeQuery query, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the reference values available as filters, for populating a client's
    /// dropdowns from the database rather than a hard-coded list.
    /// </summary>
    Task<BinRangeFilterOptions> GetFilterOptionsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns one page of live BIN ranges whose stored card scheme disagrees with the
    /// scheme the detector assigns to the prefix - the rows an admin should review and
    /// correct. Each item carries the detector's opinion in
    /// <see cref="BinRangeListItem.DetectedScheme"/>.
    /// </summary>
    Task<PagedResult<BinRangeListItem>> GetSchemeMismatchesAsync(
        int page, int pageSize, CancellationToken cancellationToken = default);

    /// <summary>
    /// Total number of scheme-mismatched live ranges, for the Home badge. Same scan as
    /// <see cref="GetSchemeMismatchesAsync"/>, without the projection.
    /// </summary>
    Task<int> CountSchemeMismatchesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// The detector's reading of a prefix being typed, for an editor that suggests the scheme and
    /// warns when the chosen one disagrees. Synchronous: the detector is a lookup table, so this
    /// touches no storage.
    /// </summary>
    /// <exception cref="ArgumentException">
    /// The prefix is blank, holds a non-digit, or is outside 6-19 digits.
    /// </exception>
    SchemeHint DetectScheme(string prefix, string? declaredScheme);
}
