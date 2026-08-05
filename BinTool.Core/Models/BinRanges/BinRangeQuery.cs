namespace BinTool.Core.Models.BinRanges;

/// <summary>
/// Filters and paging for a BIN range search. Every filter is optional; omitting all
/// of them lists everything that has not been deleted.
/// </summary>
public class BinRangeQuery
{
    /// <summary>
    /// Largest page the API will return in one call, so a wide-open query cannot pull
    /// the whole table into memory.
    /// </summary>
    public const int MaxPageSize = 200;

    public const int DefaultPageSize = 25;

    /// <summary>
    /// Matches prefixes that start with these digits. "4000" finds 400001 and 40000123.
    /// </summary>
    public string? Prefix { get; set; }

    /// <summary>
    /// Card scheme name, matched case-insensitively (for example "Visa").
    /// </summary>
    public string? CardScheme { get; set; }

    /// <summary>
    /// Product type name, matched case-insensitively (for example "Consumer").
    /// </summary>
    public string? ProductType { get; set; }

    /// <summary>
    /// Funding type name, matched case-insensitively (for example "Credit").
    /// </summary>
    public string? FundingType { get; set; }

    /// <summary>
    /// ISO 3166-1 alpha-2 country code, matched case-insensitively (for example "BG").
    /// </summary>
    public string? CountryCode { get; set; }

    /// <summary>
    /// Restricts the results to one status. Left unset, deleted ranges are excluded and
    /// everything else is returned; pass <c>Deleted</c> to see the soft-deleted ones.
    /// </summary>
    public BinRangeStatus? Status { get; set; }

    /// <summary>
    /// User name of whoever added the range, matched case-insensitively - normally the
    /// person who ran the import. The literal <c>system</c> selects rows written with no
    /// user signed in.
    /// </summary>
    public string? CreatedBy { get; set; }

    /// <summary>
    /// 1-based page number. Values below 1 are treated as 1.
    /// </summary>
    public int Page { get; set; } = 1;

    /// <summary>
    /// Rows per page, capped at <see cref="MaxPageSize"/>.
    /// </summary>
    public int PageSize { get; set; } = DefaultPageSize;
}
