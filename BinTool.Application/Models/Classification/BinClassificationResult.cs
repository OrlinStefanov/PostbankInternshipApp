using BinTool.Application.Models.Commission;

namespace BinTool.Application.Models.Classification;

public class BinClassificationResult
{
    public string Bin { get; set; } = string.Empty;

    public bool Matched { get; set; }

    public string? MatchedPrefix { get; set; }

    public string? CardScheme { get; set; }

    public string? ProductType { get; set; }

    public string? FundingType { get; set; }

    public string? CountryCode { get; set; }

    public string? CountryName { get; set; }

    public string? Region { get; set; }

    public DateTime? ValidFrom { get; set; }

    public DateTime? ValidTo { get; set; }

    public string? DetectedScheme { get; set; }

    public CommissionCalculation? Commission { get; set; }
}
