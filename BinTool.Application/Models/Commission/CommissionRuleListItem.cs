namespace BinTool.Application.Models.Commission;

public class CommissionRuleListItem
{
    public int Id { get; set; }

    public string RuleName { get; set; } = string.Empty;

    public int? CardSchemeId { get; set; }

    public string? CardSchemeName { get; set; }

    public int? ProductTypeId { get; set; }

    public string? ProductTypeName { get; set; }

    public int? FundingTypeId { get; set; }

    public string? FundingTypeName { get; set; }

    public int? RegionId { get; set; }

    public string? RegionName { get; set; }

    public int CurrencyId { get; set; }

    public string CurrencyCode { get; set; } = string.Empty;

    public decimal PercentageRate { get; set; }

    public decimal FixedAmount { get; set; }

    public decimal MinimumFee { get; set; }

    public int Priority { get; set; }

    public int PriorityScore { get; set; }

    public int Specificity { get; set; }

    public DateTime ValidFrom { get; set; }

    public DateTime? ValidTo { get; set; }

    public bool IsActive { get; set; }

    public CommissionRuleStatus Status { get; set; }

    public bool IsDefault { get; set; }

    public DateTime CreatedAt { get; set; }

    public string? CreatedBy { get; set; }

    public DateTime UpdatedAt { get; set; }

    public string? UpdatedBy { get; set; }

    public DateTime? DeletedAt { get; set; }

    public string? DeletedBy { get; set; }
}
