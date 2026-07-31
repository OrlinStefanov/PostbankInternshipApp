namespace BinTool.Core.Entities;

/// <summary>
/// Commission/fee rule for card transactions
/// Supports percentage + fixed amount with minimum fee
/// </summary>
public class CommissionRule
{
    public int Id { get; set; }

    /// <summary>
    /// Card scheme this rule applies to (or null for wildcard/any scheme)
    /// </summary>
    public CardScheme? Scheme { get; set; }

    /// <summary>
    /// Product type this rule applies to (or null for wildcard/any product)
    /// </summary>
    public ProductType? ProductType { get; set; }

    /// <summary>
    /// Region this rule applies to (or null for wildcard/any region)
    /// </summary>
    public int? RegionId { get; set; }

    /// <summary>
    /// Navigation property for the region
    /// </summary>
    public Region? Region { get; set; }

    /// <summary>
    /// Percentage rate (e.g., 0.85 for 0.85%)
    /// </summary>
    public decimal PercentageRate { get; set; }

    /// <summary>
    /// Fixed amount in the transaction currency (e.g., 0.12 BGN)
    /// </summary>
    public decimal FixedAmount { get; set; }

    /// <summary>
    /// Currency code for the fixed amount (e.g., "BGN", "EUR")
    /// </summary>
    public string Currency { get; set; } = "EUR";

    /// <summary>
    /// Minimum fee that should be charged (e.g., 0.20 BGN)
    /// </summary>
    public decimal MinimumFee { get; set; }

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

    /// <summary>
    /// Description of this rule
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Timestamp when this record was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Timestamp when this record was last updated
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// User ID of who created/last updated this record
    /// </summary>
    public string? LastModifiedBy { get; set; }
}
