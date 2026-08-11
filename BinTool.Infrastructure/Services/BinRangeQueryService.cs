using BinTool.Domain.Entities;
using BinTool.Application.Models.BinRanges;
using BinTool.Application.Abstractions;
using BinTool.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BinTool.Infrastructure.Services;

public class BinRangeQueryService : IBinRangeQueryService
{
    private readonly AppDbContext _db;
    private readonly ICardSchemeDetector _detector;

    public BinRangeQueryService(AppDbContext db, ICardSchemeDetector detector)
    {
        _db = db;
        _detector = detector;
    }

    public async Task<PagedResult<BinRangeListItem>> SearchAsync(
        BinRangeQuery query, CancellationToken cancellationToken = default)
    {
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(
            query.PageSize <= 0 ? BinRangeQuery.DefaultPageSize : query.PageSize,
            1, BinRangeQuery.MaxPageSize);

        var today = DateTime.UtcNow.Date;
        var rows = ApplyFilters(_db.BinRanges.AsNoTracking(), query, today);

        // Counted before paging so the pager knows how many pages exist.
        var totalCount = await rows.CountAsync(cancellationToken);

        var items = await rows
            .OrderBy(b => b.Prefix)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(BinRangeProjection.ToListItem(today))
            .ToListAsync(cancellationToken);

        // Flag the mismatched rows on the ordinary listing too, so a browsing user sees
        // the same warning the vulnerabilities page does. Runs on the sliced page only -
        // capped by BinRangeQuery.MaxPageSize, so this is at most a couple of hundred
        // Detect() calls.
        foreach (var item in items)
        {
            var detected = _detector.Detect(item.Prefix);
            var detectedName = _detector.DisplayName(detected);
            if (detectedName is null) continue;
            if (_detector.Matches(detected, item.CardScheme)) continue;

            item.DetectedScheme = detectedName;
        }

        return new PagedResult<BinRangeListItem>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    public async Task<BinRangeFilterOptions> GetFilterOptionsAsync(
        CancellationToken cancellationToken = default)
    {
        return new BinRangeFilterOptions
        {
            CardSchemes = await _db.CardSchemes.AsNoTracking()
                .Where(x => !x.IsDeleted).OrderBy(x => x.Name)
                .Select(x => x.Name).ToListAsync(cancellationToken),

            ProductTypes = await _db.ProductTypes.AsNoTracking()
                .Where(x => !x.IsDeleted).OrderBy(x => x.Name)
                .Select(x => x.Name).ToListAsync(cancellationToken),

            FundingTypes = await _db.FundingTypes.AsNoTracking()
                .Where(x => !x.IsDeleted).OrderBy(x => x.Name)
                .Select(x => x.Name).ToListAsync(cancellationToken),

            Countries = await _db.Countries.AsNoTracking()
                .Where(x => !x.IsDeleted).OrderBy(x => x.Name)
                .Select(x => new CountryOption { IsoCode = x.IsoCode, Name = x.Name })
                .ToListAsync(cancellationToken),

            // Every account that has added a range, deleted rows included, so the "added
            // by" filter can reach a range no matter its current state.
            Creators = await _db.BinRanges.AsNoTracking()
                .Where(b => b.CreatedBy != null)
                .Select(b => b.CreatedBy!)
                .Distinct().OrderBy(name => name)
                .ToListAsync(cancellationToken)
        };
    }

    /// <summary>
    /// Narrows the query by whichever filters were supplied. Status is expressed as a
    /// where clause per case rather than by filtering the projection, so paging and the
    /// total count both run in the database.
    /// </summary>
    private static IQueryable<BinRange> ApplyFilters(
        IQueryable<BinRange> rows, BinRangeQuery query, DateTime today)
    {
        var prefix = query.Prefix?.Trim();
        if (!string.IsNullOrEmpty(prefix))
        {
            rows = rows.Where(b => b.Prefix.StartsWith(prefix));
        }

        // Lower-cased comparison rather than an exact match: SQLite compares text
        // case-sensitively by default, and "visa" should find Visa.
        var cardScheme = Normalize(query.CardScheme);
        if (cardScheme is not null)
        {
            rows = rows.Where(b => b.CardScheme!.Name.ToLower() == cardScheme);
        }

        var productType = Normalize(query.ProductType);
        if (productType is not null)
        {
            rows = rows.Where(b => b.ProductType!.Name.ToLower() == productType);
        }

        var fundingType = Normalize(query.FundingType);
        if (fundingType is not null)
        {
            rows = rows.Where(b => b.FundingType!.Name.ToLower() == fundingType);
        }

        var countryCode = Normalize(query.CountryCode);
        if (countryCode is not null)
        {
            rows = rows.Where(b => b.Country!.IsoCode.ToLower() == countryCode);
        }

        var createdBy = Normalize(query.CreatedBy);
        if (createdBy is not null)
        {
            rows = rows.Where(b => b.CreatedBy != null && b.CreatedBy.ToLower() == createdBy);
        }

        return query.Status switch
        {
            BinRangeStatus.Deleted => rows.Where(b => b.IsDeleted),

            BinRangeStatus.Expired => rows.Where(b =>
                !b.IsDeleted && b.ValidTo != null && b.ValidTo < today),

            BinRangeStatus.Scheduled => rows.Where(b =>
                !b.IsDeleted && b.ValidFrom > today),

            BinRangeStatus.Active => rows.Where(b =>
                !b.IsDeleted && b.ValidFrom <= today && (b.ValidTo == null || b.ValidTo >= today)),

            // No status filter: show everything that still exists.
            _ => rows.Where(b => !b.IsDeleted)
        };
    }

    public async Task<PagedResult<BinRangeListItem>> GetSchemeMismatchesAsync(
        int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var actualPage = Math.Max(1, page);
        var actualSize = Math.Clamp(
            pageSize <= 0 ? BinRangeQuery.DefaultPageSize : pageSize,
            1, BinRangeQuery.MaxPageSize);

        var mismatches = await FindMismatchesAsync(cancellationToken);

        var pageIds = mismatches
            .OrderBy(m => m.Prefix, StringComparer.Ordinal)
            .Skip((actualPage - 1) * actualSize)
            .Take(actualSize)
            .ToList();

        // Second query for the full projection - only for the sliced ids, so the payload
        // stays small even when the mismatch set is large.
        var today = DateTime.UtcNow.Date;
        var ids = pageIds.Select(m => m.Id).ToList();
        var items = await _db.BinRanges.AsNoTracking()
            .Where(b => ids.Contains(b.BinRangeId))
            .Select(BinRangeProjection.ToListItem(today))
            .ToListAsync(cancellationToken);

        // Attach the detector's opinion; order the response by prefix to match pageIds.
        var byId = pageIds.ToDictionary(m => m.Id, m => m.DetectedName);
        foreach (var item in items)
        {
            item.DetectedScheme = byId[item.BinRangeId];
        }

        return new PagedResult<BinRangeListItem>
        {
            Items = items.OrderBy(i => i.Prefix, StringComparer.Ordinal).ToList(),
            Page = actualPage,
            PageSize = actualSize,
            TotalCount = mismatches.Count
        };
    }

    public async Task<int> CountSchemeMismatchesAsync(CancellationToken cancellationToken = default)
    {
        var mismatches = await FindMismatchesAsync(cancellationToken);
        return mismatches.Count;
    }

    /// <summary>
    /// Scans every live BIN range through the detector, returning the ones whose stored
    /// scheme contradicts the detector's opinion. Selects only the three columns needed to
    /// judge each row, so the scan stays cheap even at tens of thousands of ranges.
    /// </summary>
    private async Task<List<MismatchRow>> FindMismatchesAsync(CancellationToken cancellationToken)
    {
        var rows = await _db.BinRanges.AsNoTracking()
            .Where(b => !b.IsDeleted)
            .Select(b => new { b.BinRangeId, b.Prefix, SchemeName = b.CardScheme!.Name })
            .ToListAsync(cancellationToken);

        var mismatches = new List<MismatchRow>();
        foreach (var row in rows)
        {
            var detected = _detector.Detect(row.Prefix);
            var detectedName = _detector.DisplayName(detected);
            if (detectedName is null) continue; // detector cannot judge this prefix
            if (_detector.Matches(detected, row.SchemeName)) continue;

            mismatches.Add(new MismatchRow(row.BinRangeId, row.Prefix, detectedName));
        }

        return mismatches;
    }

    private readonly record struct MismatchRow(int Id, string Prefix, string DetectedName);

    private static string? Normalize(string? value)
    {
        var trimmed = value?.Trim();
        return string.IsNullOrEmpty(trimmed) ? null : trimmed.ToLowerInvariant();
    }
}
