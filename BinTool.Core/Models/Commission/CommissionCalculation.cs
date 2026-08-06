namespace BinTool.Core.Models.Commission;

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

    /// <summary>The transaction amount the fee was worked out for.</summary>
    public decimal Amount { get; set; }

    public decimal PercentageRate { get; set; }

    public decimal FixedAmount { get; set; }

    public decimal MinimumFee { get; set; }

    /// <summary>Percentage part plus fixed amount, before the minimum-fee floor is applied.</summary>
    public decimal RawFee { get; set; }

    /// <summary>The final fee: <see cref="RawFee"/> raised to <see cref="MinimumFee"/>, rounded.</summary>
    public decimal Fee { get; set; }

    /// <summary>True when the minimum fee floor raised the result above the calculated amount.</summary>
    public bool MinimumApplied { get; set; }

    /// <summary>A plain-language account of which rule was chosen and why.</summary>
    public string Reason { get; set; } = string.Empty;
}
