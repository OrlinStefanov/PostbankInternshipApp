namespace BinTool.Application.Models.Audit;

public sealed record CurrencySnapshot(
    string Code,
    string Name,
    decimal RateToEur,
    bool IsActive,
    bool IsDeleted);
