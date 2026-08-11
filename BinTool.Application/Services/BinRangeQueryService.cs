using BinTool.Application.Abstractions;
using BinTool.Application.Models.BinRanges;

namespace BinTool.Application.Services;

/// <summary>
/// Browsing BIN ranges, and the scheme-mismatch report. The database narrows and pages; what
/// is decided here is the detector's opinion, which is a judgement about the data rather
/// than something a query can express.
/// </summary>
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

        // Flag mismatched rows on the ordinary listing too, so a browsing user sees the
        // same warning the vulnerabilities page does. Runs on the sliced page only - capped
        // by MaxPageSize, so this is at most a couple of hundred Detect() calls.
        foreach (var item in result.Items)
        {
            item.DetectedScheme = MismatchedSchemeName(item.Prefix, item.CardScheme);
        }

        return result;
    }

    public Task<BinRangeFilterOptions> GetFilterOptionsAsync(
        CancellationToken cancellationToken = default) =>
        _ranges.GetFilterOptionsAsync(cancellationToken);

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

        // Second read for the full projection, only for the sliced ids, so the payload
        // stays small even when the mismatch set is large.
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

    /// <summary>
    /// Every live range whose stored scheme contradicts the detector. A prefix the detector
    /// cannot place is not a mismatch - it is an absence of an opinion, and reporting it as
    /// a fault would bury the real ones.
    /// </summary>
    private async Task<List<Mismatch>> FindMismatchesAsync(CancellationToken cancellationToken)
    {
        var rows = await _ranges.ListLivePrefixSchemesAsync(cancellationToken);

        var mismatches = new List<Mismatch>();
        foreach (var row in rows)
        {
            if (MismatchedSchemeName(row.Prefix, row.SchemeName) is { } detectedName)
            {
                mismatches.Add(new Mismatch(row.Id, row.Prefix, detectedName));
            }
        }

        return mismatches;
    }

    /// <summary>
    /// The name of the scheme the digits say this prefix belongs to, when that contradicts
    /// the stored one. Null when they agree, or when the detector has no opinion.
    /// </summary>
    private string? MismatchedSchemeName(string prefix, string storedScheme)
    {
        var detected = _detector.Detect(prefix);

        var detectedName = _detector.DisplayName(detected);
        if (detectedName is null) return null;

        return _detector.Matches(detected, storedScheme) ? null : detectedName;
    }

    private static (int Page, int PageSize) Paging(int page, int pageSize) =>
        (Math.Max(1, page),
         Math.Clamp(
             pageSize <= 0 ? BinRangeQuery.DefaultPageSize : pageSize,
             1, BinRangeQuery.MaxPageSize));

    private readonly record struct Mismatch(int Id, string Prefix, string DetectedName);
}
