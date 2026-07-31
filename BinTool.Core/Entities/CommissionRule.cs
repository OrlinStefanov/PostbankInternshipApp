namespace BinTool.Core.Entities;

/// <summary>
/// Commission/fee rule for card transactions
/// Supports percentage + fixed amount with minimum fee
/// </summary>
public class CommissionRule
{
    public int CommissionRuleId { get; set; }

    /// <summary>
    /// Descriptive name for this rule
    /// </summary>
    public string RuleName { get; set; } = string.Empty;

    /// <summary>
    /// Percentage rate (e.g., 0.85 for 0.85%)
    /// </summary>
    public decimal PercentageRate { get; set; }

    /// <summary>
    /// Fixed amount in the transaction currency (e.g., 0.12 BGN)
    /// </summary>
    public decimal FixedAmount { get; set; }

    /// <summary>
    /// Minimum fee that should be charged (e.g., 0.20 BGN)
    /// </summary>
    public decimal MinimumFee { get; set; }

    /// <summary>
    /// Priority for rule resolution (higher number = higher priority)
    /// </summary>
    public int Priority { get; set; }

    /// <summary>
    /// Rule is valid from this date (inclusive)
    /// </summary>
    public DateTime ValidFrom { get; set; }

    /// <summary>
    /// Rule is valid until this date (inclusive). Null means open-ended/no expiration
    /// </summary>
    public DateTime? ValidTo { get; set; }

    /// <summary>
    /// Whether this rule is currently active
    /// </summary>
    public bool IsActive { get; set; } = true;

    #region Audit Fields

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public string? CreatedBy { get; set; }

    public string? UpdatedBy { get; set; }

    #endregion

    #region Soft Delete

    public bool IsDeleted { get; set; }

    public DateTime? DeletedAt { get; set; }

    public string? DeletedBy { get; set; }

    #endregion

    #region Navigation Properties

    /// <summary>
    /// Criteria for this rule
    /// </summary>
    public ICollection<RuleCriteria> RuleCriteria { get; set; } = new List<RuleCriteria>();

    /// <summary>
    /// Default rule flag
    /// </summary>
    public DefaultRule? DefaultRule { get; set; }

    #endregion
}
