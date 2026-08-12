namespace BinTool.Application.Models.BinRanges;

public readonly record struct BinReferenceNames(
    NamedReference? CardScheme,
    NamedReference? ProductType,
    NamedReference? FundingType,
    NamedReference? Country);
