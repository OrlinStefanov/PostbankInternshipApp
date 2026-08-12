namespace BinTool.Application.Models.Import;

public sealed record ReferenceTables(
    IReadOnlyDictionary<string, int> CardSchemeIds,
    IReadOnlyDictionary<string, int> ProductTypeIds,
    IReadOnlyDictionary<string, int> FundingTypeIds,
    IReadOnlyDictionary<string, int> CountryIds,
    IReadOnlyDictionary<int, string> CardSchemeNames,
    IReadOnlyDictionary<int, string> ProductTypeNames,
    IReadOnlyDictionary<int, string> FundingTypeNames,
    IReadOnlyDictionary<int, string> CountryCodes);
