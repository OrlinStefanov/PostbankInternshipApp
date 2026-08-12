using System.ComponentModel.DataAnnotations;
using BinTool.Domain.Common;

namespace BinTool.Application.Models.Commission;

public class CommissionRuleInput
{
    /// <summary>A human-readable label.</summary>
    [Required(ErrorMessage = "A rule name is required.")]
    [StringLength(200, MinimumLength = 1,
        ErrorMessage = "The rule name must be 1 to 200 characters.")]
    public string RuleName { get; set; } = string.Empty;

    /// <summary>Card scheme this rule applies to, or null for any scheme.</summary>
    public int? CardSchemeId { get; set; }

    /// <summary>Product type this rule applies to, or null for any product.</summary>
    public int? ProductTypeId { get; set; }

    /// <summary>Funding type this rule applies to, or null for any funding.</summary>
    public int? FundingTypeId { get; set; }

    /// <summary>Region this rule applies to, or null for any region.</summary>
    public int? RegionId { get; set; }

    /// <summary>The currency the fixed amount and minimum fee are denominated in.</summary>
    public int CurrencyId { get; set; } = 1;

    /// <summary>Percentage rate as a percent value: 0.85 means 0.85%.</summary>
    [Range(0, 100, ErrorMessage = "The percentage rate must be between 0 and 100.")]
    public decimal PercentageRate { get; set; }

    /// <summary>Flat amount added on top of the percentage, in the transaction currency.</summary>
    [Range(0, double.MaxValue, ErrorMessage = "The fixed amount cannot be negative.")]
    public decimal FixedAmount { get; set; }

    /// <summary>The floor: the fee is raised to this when the calculated amount is lower.</summary>
    [Range(0, double.MaxValue, ErrorMessage = "The minimum fee cannot be negative.")]
    public decimal MinimumFee { get; set; }

    /// <summary>
    /// The primary ranking for rule resolution: among the rules that match a card and are valid on
    /// the date, the one with the highest priority wins.
    /// </summary>
    [Range(CommissionRuleLimits.MinPriority, CommissionRuleLimits.MaxPriority,
        ErrorMessage = "Priority must be between 0 and 100.")]
    public int Priority { get; set; }

    /// <summary>Secondary tiebreak after <see cref="Priority"/>.</summary>
    [Range(CommissionRuleLimits.MinPriorityScore, CommissionRuleLimits.MaxPriorityScore,
        ErrorMessage = "Priority score must be between 0 and 100.")]
    public int? PriorityScore { get; set; }

    /// <summary>The date the rule starts applying (inclusive).</summary>
    [Required(ErrorMessage = "A ValidFrom date is required.")]
    public DateTime ValidFrom { get; set; }

    /// <summary>The date the rule stops applying (inclusive), or null for open-ended.</summary>
    public DateTime? ValidTo { get; set; }

    /// <summary>Whether the rule takes part in resolution.</summary>
    public bool IsActive { get; set; } = true;
}
