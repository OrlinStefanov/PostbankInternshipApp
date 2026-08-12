namespace BinTool.Application.Models.Commission;

public class CommissionCalculation
{
    /// <summary>The rule that was applied.</summary>
    public int? AppliedRuleId { get; set; }

    /// <summary>The name of the applied rule (or the default rule, when a fallback).</summary>
    public string? AppliedRuleName { get; set; }

    /// <summary>
    /// True when no rule matched the card's attributes and the configured default rule was used
    /// instead.
    /// </summary>
    public bool IsFallback { get; set; }

    /// <summary>
    /// The transaction amount the fee was worked out for, expressed in the rule's currency (<see
    /// cref="CurrencyCode"/>).
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>The amount as the caller supplied it, in <see cref="InputCurrencyCode"/>.</summary>
    public decimal InputAmount { get; set; }

    /// <summary>ISO-4217 code the amount was supplied in, e.g. "EUR".</summary>
    public string InputCurrencyCode { get; set; } = "EUR";

    /// <summary>ISO-4217 code the fee is denominated in - the applied rule's currency.</summary>
    public string CurrencyCode { get; set; } = "EUR";

    /// <summary>The euro value of one unit of <see cref="CurrencyCode"/>.</summary>
    public decimal EurRate { get; set; } = 1m;

    public decimal PercentageRate { get; set; }

    public decimal FixedAmount { get; set; }

    public decimal MinimumFee { get; set; }

    /// <summary>
    /// Percentage part plus fixed amount, before the minimum-fee floor is applied.
    /// </summary>
    public decimal RawFee { get; set; }

    /// <summary>
    /// The final fee: <see cref="RawFee"/> raised to <see cref="MinimumFee"/>, rounded.
    /// </summary>
    public decimal Fee { get; set; }

    /// <summary>
    /// True when the minimum fee floor raised the result above the calculated amount.
    /// </summary>
    public bool MinimumApplied { get; set; }

    // ---- Euro equivalents ------------------------------------------------------
    // The same figures converted at EurRate, so fees priced in different currencies can be
    // compared. Equal to their native counterparts when the rule is already in euro.

    /// <summary><see cref="Amount"/> in euro.</summary>
    public decimal AmountEur { get; set; }

    /// <summary><see cref="FixedAmount"/> in euro.</summary>
    public decimal FixedAmountEur { get; set; }

    /// <summary><see cref="MinimumFee"/> in euro.</summary>
    public decimal MinimumFeeEur { get; set; }

    /// <summary><see cref="RawFee"/> in euro.</summary>
    public decimal RawFeeEur { get; set; }

    /// <summary><see cref="Fee"/> in euro.</summary>
    public decimal FeeEur { get; set; }

    /// <summary>A plain-language account of which rule was chosen and why.</summary>
    public string Reason { get; set; } = string.Empty;
}
