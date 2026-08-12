using BinTool.Application.Models.BinRanges;

namespace BinTool.Application.Abstractions;

public interface IBinRangeQueryService
{
    /// <summary>
    /// Returns one page of BIN ranges matching the filters, ordered by prefix, with the lookup ids
    /// resolved to names and a status derived from the validity dates.
    /// </summary>
    Task<PagedResult<BinRangeListItem>> SearchAsync(
        BinRangeQuery query, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the reference values available as filters, for populating a client's dropdowns from
    /// the database rather than a hard-coded list.
    /// </summary>
    Task<BinRangeFilterOptions> GetFilterOptionsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns one page of live BIN ranges whose stored card scheme disagrees with the scheme the
    /// detector assigns to the prefix - the rows an admin should review and correct.
    /// </summary>
    Task<PagedResult<BinRangeListItem>> GetSchemeMismatchesAsync(
        int page, int pageSize, CancellationToken cancellationToken = default);

    /// <summary>Total number of scheme-mismatched live ranges, for the Home badge.</summary>
    Task<int> CountSchemeMismatchesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// The detector's reading of a prefix being typed, for an editor that suggests the scheme and
    /// warns when the chosen one disagrees.
    /// </summary>
    SchemeHint DetectScheme(string prefix, string? declaredScheme);
}
