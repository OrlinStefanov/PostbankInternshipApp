namespace BinTool.Application.Models.BinRanges;

public class BinRangeFilterOptions
{
    public List<string> CardSchemes { get; set; } = new();

    public List<string> ProductTypes { get; set; } = new();

    public List<string> FundingTypes { get; set; } = new();

    public List<CountryOption> Countries { get; set; } = new();

    public List<string> Creators { get; set; } = new();
}
