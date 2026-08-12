namespace BinTool.Application.Services.BinImport;

internal readonly record struct ParsedRow(
    string Prefix, string CardScheme, string ProductType, string FundingType,
    string CountryCode, DateTime ValidFrom, DateTime? ValidTo);
