namespace BinTool.Application.Models.BinRanges;

public sealed record MatchedRange(
    string Prefix,
    int CardSchemeId,
    string CardScheme,
    int ProductTypeId,
    string ProductType,
    int FundingTypeId,
    string FundingType,
    string CountryCode,
    string CountryName,
    int RegionId,
    string Region,
    DateTime ValidFrom,
    DateTime? ValidTo);
