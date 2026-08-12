namespace BinTool.Application.Models.Audit;

public sealed record CommissionRuleSnapshot(
    string RuleName,
    string CardScheme,
    string ProductType,
    string FundingType,
    string Region,
    string Currency,
    decimal PercentageRate,
    decimal FixedAmount,
    decimal MinimumFee,
    int Priority,
    int PriorityScore,
    string ValidFrom,
    string? ValidTo,
    bool IsActive,
    bool IsDeleted)
{
    public const string AnyValue = "(any)";

    public static string Date(DateTime value) => value.ToString("yyyy-MM-dd");
}
