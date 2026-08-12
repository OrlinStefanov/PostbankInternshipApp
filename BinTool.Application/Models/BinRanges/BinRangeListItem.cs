namespace BinTool.Application.Models.BinRanges;

public class BinRangeListItem
{
    public int BinRangeId { get; set; }

    public string Prefix { get; set; } = string.Empty;

    public int PrefixLength { get; set; }

    public string CardScheme { get; set; } = string.Empty;

    public string ProductType { get; set; } = string.Empty;

    public string FundingType { get; set; } = string.Empty;

    public string CountryCode { get; set; } = string.Empty;

    public string CountryName { get; set; } = string.Empty;

    public string Region { get; set; } = string.Empty;

    public DateTime ValidFrom { get; set; }

    public DateTime? ValidTo { get; set; }

    public BinRangeStatus Status { get; set; }

    public DateTime CreatedAt { get; set; }

    public string? CreatedBy { get; set; }

    public DateTime UpdatedAt { get; set; }

    public string? UpdatedBy { get; set; }

    public string? DetectedScheme { get; set; }
}
