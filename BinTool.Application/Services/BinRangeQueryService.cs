using BinTool.Application.Abstractions;
using BinTool.Application.Models.BinRanges;
using BinTool.Application.Services.BinRangeQueries;

namespace BinTool.Application.Services;

public class BinRangeQueryService : IBinRangeQueryService
{
    private readonly IBinRangeRepository _ranges;
    private readonly ICardSchemeDetector _detector;

    public BinRangeQueryService(IBinRangeRepository ranges, ICardSchemeDetector detector)
    {
        _ranges = ranges;
        _detector = detector;
    }

    public async Task<PagedResult<BinRangeListItem>> SearchAsync(
        BinRangeQuery query, CancellationToken cancellationToken = default)
    {
        var (page, pageSize) = Paging(query.Page, query.PageSize);

        var result = await _ranges.SearchAsync(
            query, page, pageSize, DateTime.UtcNow.Date, cancellationToken);

        foreach (var item in result.Items)
        {
            item.DetectedScheme = _detector.MismatchedName(item.Prefix, item.CardScheme);
        }

        return result;
    }

    public Task<BinRangeFilterOptions> GetFilterOptionsAsync(
        CancellationToken cancellationToken = default) =>
        _ranges.GetFilterOptionsAsync(cancellationToken);

    public SchemeHint DetectScheme(string prefix, string? declaredScheme)
    {
        var normalized = BinPrefix.Normalize(prefix);

        var detected = _detector.Detect(normalized);

        return new SchemeHint(
            _detector.DisplayName(detected),
            _detector.Matches(detected, declaredScheme));
    }

    public async Task<PagedResult<BinRangeListItem>> GetSchemeMismatchesAsync(
        int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var (actualPage, actualSize) = Paging(page, pageSize);

        var mismatches = await FindMismatchesAsync(cancellationToken);

        var pageRows = mismatches
            .OrderBy(m => m.Prefix, StringComparer.Ordinal)
            .Skip((actualPage - 1) * actualSize)
            .Take(actualSize)
            .ToList();

        var items = await _ranges.GetManyAsync(
            pageRows.Select(m => m.Id).ToList(), DateTime.UtcNow.Date, cancellationToken);

        var detectedById = pageRows.ToDictionary(m => m.Id, m => m.DetectedName);

        foreach (var item in items)
        {
            item.DetectedScheme = detectedById[item.BinRangeId];
        }

        return new PagedResult<BinRangeListItem>
        {
            Items = items.OrderBy(i => i.Prefix, StringComparer.Ordinal).ToList(),
            Page = actualPage,
            PageSize = actualSize,
            TotalCount = mismatches.Count
        };
    }

    public async Task<int> CountSchemeMismatchesAsync(CancellationToken cancellationToken = default) =>
        (await FindMismatchesAsync(cancellationToken)).Count;

    private async Task<List<Mismatch>> FindMismatchesAsync(CancellationToken cancellationToken)
    {
        var rows = await _ranges.ListLivePrefixSchemesAsync(cancellationToken);

        var mismatches = new List<Mismatch>();
        foreach (var row in rows)
        {
            if (_detector.MismatchedName(row.Prefix, row.SchemeName) is { } detectedName)
            {
                mismatches.Add(new Mismatch(row.Id, row.Prefix, detectedName));
            }
        }

        return mismatches;
    }

    private static (int Page, int PageSize) Paging(int page, int pageSize) =>
        (Math.Max(1, page),
         Math.Clamp(
             pageSize <= 0 ? BinRangeQuery.DefaultPageSize : pageSize,
             1, BinRangeQuery.MaxPageSize));
}
