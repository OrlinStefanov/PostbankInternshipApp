using System.ComponentModel.DataAnnotations;
using BinTool.Domain.Common;

namespace BinTool.Application.Models.Commission;

public class CommissionRuleInput
{
    [Required(ErrorMessage = "A rule name is required.")]
    [StringLength(200, MinimumLength = 1,
        ErrorMessage = "The rule name must be 1 to 200 characters.")]
    public string RuleName { get; set; } = string.Empty;

    public int? CardSchemeId { get; set; }

    public int? ProductTypeId { get; set; }

    public int? FundingTypeId { get; set; }

    public int? RegionId { get; set; }

    public int CurrencyId { get; set; } = 1;

    [Range(0, 100, ErrorMessage = "The percentage rate must be between 0 and 100.")]
    public decimal PercentageRate { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "The fixed amount cannot be negative.")]
    public decimal FixedAmount { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "The minimum fee cannot be negative.")]
    public decimal MinimumFee { get; set; }

    [Range(CommissionRuleLimits.MinPriority, CommissionRuleLimits.MaxPriority,
        ErrorMessage = "Priority must be between 0 and 100.")]
    public int Priority { get; set; }

    [Range(CommissionRuleLimits.MinPriorityScore, CommissionRuleLimits.MaxPriorityScore,
        ErrorMessage = "Priority score must be between 0 and 100.")]
    public int? PriorityScore { get; set; }

    [Required(ErrorMessage = "A ValidFrom date is required.")]
    public DateTime ValidFrom { get; set; }

    public DateTime? ValidTo { get; set; }

    public bool IsActive { get; set; } = true;
}
