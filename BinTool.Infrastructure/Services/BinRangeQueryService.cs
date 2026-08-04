using BinTool.Core.Entities;
using BinTool.Core.Models.BinRanges;
using BinTool.Core.Services;
using BinTool.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BinTool.Infrastructure.Services;

public class BinRangeQueryService : IBinRangeQueryService
{
    private readonly AppDbContext _db;

    public BinRangeQueryService(AppDbContext db)
    {
        _db = db;
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
            .Select(b => new BinRangeListItem
            {
                BinRangeId = b.BinRangeId,
                Prefix = b.Prefix,
                PrefixLength = b.PrefixLength,
                CardScheme = b.CardScheme!.Name,
                ProductType = b.ProductType!.Name,
                FundingType = b.FundingType!.Name,
                CountryCode = b.Country!.IsoCode,
                CountryName = b.Country.Name,
                Region = b.Country.Region!.Name,
                ValidFrom = b.ValidFrom,
                ValidTo = b.ValidTo,
                Status = b.IsDeleted
                    ? BinRangeStatus.Deleted
                    : b.ValidTo != null && b.ValidTo < today
                        ? BinRangeStatus.Expired
                        : b.ValidFrom > today
                            ? BinRangeStatus.Scheduled
                            : BinRangeStatus.Active,
                CreatedAt = b.CreatedAt,
                CreatedBy = b.CreatedBy,
                UpdatedAt = b.UpdatedAt,
                UpdatedBy = b.UpdatedBy
            })
            .ToListAsync(cancellationToken);

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

    private static string? Normalize(string? value)
    {
        var trimmed = value?.Trim();
        return string.IsNullOrEmpty(trimmed) ? null : trimmed.ToLowerInvariant();
    }
}
