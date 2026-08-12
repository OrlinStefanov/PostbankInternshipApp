namespace BinTool.Application.Models.BinRanges;

public class BinRangeFilterOptions
{
    public List<string> CardSchemes { get; set; } = new();

    public List<string> ProductTypes { get; set; } = new();

    public List<string> FundingTypes { get; set; } = new();

    public List<CountryOption> Countries { get; set; } = new();

    /// <summary>
    /// Distinct user names that have added at least one range, so a browse screen can offer an
    /// "added by" filter without hard-coding accounts.
    /// </summary>
    public List<string> Creators { get; set; } = new();
}

public class CountryOption
{
    /// <summary>ISO 3166-1 alpha-2 code, the value to send as <c>countryCode</c>.</summary>
    public string IsoCode { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;
}
