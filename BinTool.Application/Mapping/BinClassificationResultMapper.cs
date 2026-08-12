using BinTool.Application.Models.BinRanges;
using BinTool.Application.Models.Classification;

namespace BinTool.Application.Mapping;

public static class BinClassificationResultMapper
{
    public static BinClassificationResult NoMatch(string bin) => new()
    {
        Bin = bin,
        Matched = false
    };

    public static BinClassificationResult From(string bin, MatchedRange match, string? detectedScheme) => new()
    {
        Bin = bin,
        Matched = true,
        MatchedPrefix = match.Prefix,
        CardScheme = match.CardScheme,
        ProductType = match.ProductType,
        FundingType = match.FundingType,
        CountryCode = match.CountryCode,
        CountryName = match.CountryName,
        Region = match.Region,
        ValidFrom = match.ValidFrom,
        ValidTo = match.ValidTo,
        DetectedScheme = detectedScheme
    };
}
