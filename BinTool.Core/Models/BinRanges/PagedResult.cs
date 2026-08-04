namespace BinTool.Core.Models.BinRanges;

/// <summary>
/// One page of results, plus enough context for a client to render a pager without
/// a second call.
/// </summary>
public class PagedResult<T>
{
    /// <summary>
    /// The rows on this page. Empty when the page is past the end of the results.
    /// </summary>
    public List<T> Items { get; set; } = new();

    /// <summary>
    /// 1-based page number, after clamping.
    /// </summary>
    public int Page { get; set; }

    /// <summary>
    /// Rows per page, after clamping.
    /// </summary>
    public int PageSize { get; set; }

    /// <summary>
    /// Rows matching the filters across every page, not just this one.
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// Number of pages at the current page size. Zero when nothing matched.
    /// </summary>
    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);

    public bool HasPrevious => Page > 1;

    public bool HasNext => Page < TotalPages;
}
