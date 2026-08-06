using System.ComponentModel.DataAnnotations;

namespace BinTool.Core.Models.Commission;

/// <summary>
/// The values a caller supplies to add or edit a commission rule. The three id fields form
/// the rule's key: a null means a wildcard, so <c>CardSchemeId = null</c> matches every
/// scheme. A rule with all three null is the broadest possible rule and matches anything.
/// </summary>
public class CommissionRuleInput
{
    /// <summary>A human-readable label. Not unique - two rules can share a name.</summary>
    [Required(ErrorMessage = "A rule name is required.")]
    [StringLength(200, MinimumLength = 1,
        ErrorMessage = "The rule name must be 1 to 200 characters.")]
    public string RuleName { get; set; } = string.Empty;

    /// <summary>Card scheme this rule applies to, or null for any scheme.</summary>
    public int? CardSchemeId { get; set; }

    /// <summary>Product type this rule applies to, or null for any product.</summary>
    public int? ProductTypeId { get; set; }

    /// <summary>Region this rule applies to, or null for any region.</summary>
    public int? RegionId { get; set; }

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
    /// A manual tiebreak used only when two matching rules are equally specific. Higher
    /// wins. Specificity (fewer wildcards) is always decided first, so this rarely matters.
    /// </summary>
    public int Priority { get; set; }

    /// <summary>The date the rule starts applying (inclusive).</summary>
    [Required(ErrorMessage = "A ValidFrom date is required.")]
    public DateTime ValidFrom { get; set; }

    /// <summary>The date the rule stops applying (inclusive), or null for open-ended.</summary>
    public DateTime? ValidTo { get; set; }

    /// <summary>Whether the rule takes part in resolution. Inactive rules are never applied.</summary>
    public bool IsActive { get; set; } = true;
}
