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
    /// <summary>The label recorded in place of a wildcard (null) key field.</summary>
    public const string AnyValue = "(any)";

    /// <summary>Formats a date the same way across every snapshot: yyyy-MM-dd.</summary>
    public static string Date(DateTime value) => value.ToString("yyyy-MM-dd");
}
