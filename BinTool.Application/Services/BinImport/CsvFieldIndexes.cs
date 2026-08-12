namespace BinTool.Application.Services.BinImport;

internal readonly struct CsvFieldIndexes
{
    public int Prefix { get; private init; }
    public int CardScheme { get; private init; }
    public int ProductType { get; private init; }
    public int FundingType { get; private init; }
    public int CountryCode { get; private init; }
    public int ValidFrom { get; private init; }
    public int ValidTo { get; private init; }

    public static CsvFieldIndexes FromHeader(string[] header) => new()
    {
        Prefix = Array.IndexOf(header, "Prefix"),
        CardScheme = Array.IndexOf(header, "CardScheme"),
        ProductType = Array.IndexOf(header, "ProductType"),
        FundingType = Array.IndexOf(header, "FundingType"),
        CountryCode = Array.IndexOf(header, "CountryCode"),
        ValidFrom = Array.IndexOf(header, "ValidFrom"),
        ValidTo = Array.IndexOf(header, "ValidTo")
    };
}
