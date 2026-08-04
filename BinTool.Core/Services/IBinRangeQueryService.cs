using BinTool.Core.Models.BinRanges;

namespace BinTool.Core.Services;

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
}
