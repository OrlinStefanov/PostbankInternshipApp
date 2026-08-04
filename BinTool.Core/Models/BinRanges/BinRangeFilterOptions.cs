namespace BinTool.Core.Models.BinRanges;

/// <summary>
/// The reference values a client can filter by, so a browse screen can populate its
/// dropdowns from the database instead of hard-coding the lists.
/// </summary>
public class BinRangeFilterOptions
{
    public List<string> CardSchemes { get; set; } = new();

    public List<string> ProductTypes { get; set; } = new();

    public List<string> FundingTypes { get; set; } = new();

    public List<CountryOption> Countries { get; set; } = new();
}

/// <summary>
/// A country as a filter choice: the code is what the query takes, the name is what
/// a person reads.
/// </summary>
public class CountryOption
{
    /// <summary>
    /// ISO 3166-1 alpha-2 code, the value to send as <c>countryCode</c>.
    /// </summary>
    public string IsoCode { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;
}
