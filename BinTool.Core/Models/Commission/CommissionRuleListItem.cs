namespace BinTool.Core.Models.Commission;

/// <summary>
/// One commission rule as a browse listing shows it: the raw values, the resolved names
/// for the key ids, the derived status and specificity, and the audit trail.
/// </summary>
public class CommissionRuleListItem
{
    public int Id { get; set; }

    public string RuleName { get; set; } = string.Empty;

    public int? CardSchemeId { get; set; }

    /// <summary>Resolved scheme name, or null when the rule wildcards the scheme.</summary>
    public string? CardSchemeName { get; set; }

    public int? ProductTypeId { get; set; }

    /// <summary>Resolved product name, or null when the rule wildcards the product.</summary>
    public string? ProductTypeName { get; set; }

    public int? FundingTypeId { get; set; }

    /// <summary>Resolved funding name, or null when the rule wildcards the funding.</summary>
    public string? FundingTypeName { get; set; }

    public int? RegionId { get; set; }

    /// <summary>Resolved region name, or null when the rule wildcards the region.</summary>
    public string? RegionName { get; set; }

    public int CurrencyId { get; set; }

    /// <summary>The currency's ISO-4217 code the amounts are denominated in, e.g. "EUR".</summary>
    public string CurrencyCode { get; set; } = string.Empty;

    public decimal PercentageRate { get; set; }

    public decimal FixedAmount { get; set; }

    public decimal MinimumFee { get; set; }

    public int Priority { get; set; }

    /// <summary>
    /// The stored priority score — the secondary tiebreak after <see cref="Priority"/>.
    /// Can be overridden by the admin; defaults to the count of non-wildcard key fields.
    /// </summary>
    public int PriorityScore { get; set; }

    /// <summary>
    /// The system-suggested priority score: the count of non-wildcard key fields, 0 to 4.
    /// Shown alongside the stored value so the admin can see what the system would suggest.
    /// </summary>
    public int Specificity { get; set; }

    public DateTime ValidFrom { get; set; }

    public DateTime? ValidTo { get; set; }

    public bool IsActive { get; set; }

    /// <summary>Derived from the flags and the validity window.</summary>
    public CommissionRuleStatus Status { get; set; }

    /// <summary>True when this rule is the configured fallback default.</summary>
    public bool IsDefault { get; set; }

    public DateTime CreatedAt { get; set; }

    public string? CreatedBy { get; set; }

    public DateTime UpdatedAt { get; set; }

    public string? UpdatedBy { get; set; }

    public DateTime? DeletedAt { get; set; }

    public string? DeletedBy { get; set; }
}
