namespace BinTool.Application.Models.ReferenceData;

public readonly record struct ReferenceNames(
    string? Currency,
    string? CardScheme,
    string? ProductType,
    string? FundingType,
    string? Region);
