namespace BinTool.Application.Models.Commission;

/// <summary>
/// The fee worked out for a transaction, with enough of the working shown that a reader can
/// see not just the number but which rule produced it and why. Attached to a classification
/// result when the caller supplied an amount.
/// </summary>
public class CommissionCalculation
{
    /// <summary>The rule that was applied. Null only when a fallback default supplied the rates.</summary>
    public int? AppliedRuleId { get; set; }

    /// <summary>The name of the applied rule (or the default rule, when a fallback).</summary>
    public string? AppliedRuleName { get; set; }

    /// <summary>
    /// True when no rule matched the card's attributes and the configured default rule was
    /// used instead. A caller should flag the result as less certain.
    /// </summary>
    public bool IsFallback { get; set; }

    /// <summary>
    /// The transaction amount the fee was worked out for, expressed in the rule's currency
    /// (<see cref="CurrencyCode"/>). Equals <see cref="InputAmount"/> when the amount was
    /// already supplied in the rule's currency.
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>The amount as the caller supplied it, in <see cref="InputCurrencyCode"/>.</summary>
    public decimal InputAmount { get; set; }

    /// <summary>ISO-4217 code the amount was supplied in, e.g. "EUR".</summary>
    public string InputCurrencyCode { get; set; } = "EUR";

    /// <summary>
    /// ISO-4217 code the fee is denominated in - the applied rule's currency. Every native
    /// figure below (<see cref="Amount"/>, <see cref="FixedAmount"/>, <see cref="MinimumFee"/>,
    /// <see cref="RawFee"/>, <see cref="Fee"/>) is in this currency.
    /// </summary>
    public string CurrencyCode { get; set; } = "EUR";

    /// <summary>The euro value of one unit of <see cref="CurrencyCode"/>. Euro itself is 1.0.</summary>
    public decimal EurRate { get; set; } = 1m;

    public decimal PercentageRate { get; set; }

    public decimal FixedAmount { get; set; }

    public decimal MinimumFee { get; set; }

    /// <summary>Percentage part plus fixed amount, before the minimum-fee floor is applied.</summary>
    public decimal RawFee { get; set; }

    /// <summary>The final fee: <see cref="RawFee"/> raised to <see cref="MinimumFee"/>, rounded.</summary>
    public decimal Fee { get; set; }

    /// <summary>True when the minimum fee floor raised the result above the calculated amount.</summary>
    public bool MinimumApplied { get; set; }

    // ---- Euro equivalents ------------------------------------------------------
    // The same figures converted to euro at EurRate, so a reader on the euro can compare
    // fees priced in different currencies. When the rule is already in euro these equal
    // their native counterparts and EurRate is 1.

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
