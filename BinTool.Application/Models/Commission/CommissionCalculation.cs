namespace BinTool.Application.Models.Commission;

public class CommissionCalculation
{
    public int? AppliedRuleId { get; set; }

    public string? AppliedRuleName { get; set; }

    public bool IsFallback { get; set; }

    public decimal Amount { get; set; }

    public decimal InputAmount { get; set; }

    public string InputCurrencyCode { get; set; } = "EUR";

    public string CurrencyCode { get; set; } = "EUR";

    public decimal EurRate { get; set; } = 1m;

    public decimal PercentageRate { get; set; }

    public decimal FixedAmount { get; set; }

    public decimal MinimumFee { get; set; }

    public decimal RawFee { get; set; }

    public decimal Fee { get; set; }

    public bool MinimumApplied { get; set; }

    public decimal AmountEur { get; set; }

    public decimal FixedAmountEur { get; set; }

    public decimal MinimumFeeEur { get; set; }

    public decimal RawFeeEur { get; set; }

    public decimal FeeEur { get; set; }

    public string Reason { get; set; } = string.Empty;
}
