namespace BinTool.Application.Models.BinRanges;

public class BinRangeQuery
{
    public const int MaxPageSize = 200;

    public const int DefaultPageSize = 25;

    public string? Prefix { get; set; }

    public string? CardScheme { get; set; }

    public string? ProductType { get; set; }

    public string? FundingType { get; set; }

    public string? CountryCode { get; set; }

    public BinRangeStatus? Status { get; set; }

    public string? CreatedBy { get; set; }

    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = DefaultPageSize;
}
