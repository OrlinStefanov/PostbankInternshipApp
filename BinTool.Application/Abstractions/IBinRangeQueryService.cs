using BinTool.Application.Models.BinRanges;

namespace BinTool.Application.Abstractions;

public interface IBinRangeQueryService
{
    Task<PagedResult<BinRangeListItem>> SearchAsync(
        BinRangeQuery query, CancellationToken cancellationToken = default);

    Task<BinRangeFilterOptions> GetFilterOptionsAsync(CancellationToken cancellationToken = default);

    Task<PagedResult<BinRangeListItem>> GetSchemeMismatchesAsync(
        int page, int pageSize, CancellationToken cancellationToken = default);

    /// <summary>Total number of scheme-mismatched live ranges, for the Home badge.</summary>
    Task<int> CountSchemeMismatchesAsync(CancellationToken cancellationToken = default);

    SchemeHint DetectScheme(string prefix, string? declaredScheme);
}
