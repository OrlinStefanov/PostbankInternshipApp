namespace BinTool.Core.Entities;

/// <summary>
/// Bank Identification Number (BIN) range record
/// </summary>
public class BinRange
{
    public int Id { get; set; }

    /// <summary>
    /// BIN prefix (6 to 8 digits).
    /// Validation enforced at service layer: minimum 6 digits, maximum 8 digits
    /// </summary>
    public string Prefix { get; set; } = string.Empty;

    /// <summary>
    /// Card scheme for this BIN range
    /// </summary>
    public CardScheme Scheme { get; set; }

    /// <summary>
    /// Product type (Consumer, Commercial, Prepaid)
    /// </summary>
    public ProductType ProductType { get; set; }

    /// <summary>
    /// Funding type (Credit, Debit)
    /// </summary>
    public FundingType FundingType { get; set; }

    /// <summary>
    /// Issuing country code
    /// </summary>
    public int CountryId { get; set; }

    /// <summary>
    /// Navigation property for the issuing country
    /// </summary>
    public Country? Country { get; set; }

    /// <summary>
    /// Timestamp when this record was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Timestamp when this record was last updated
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// User ID of who created/last updated this record (for audit trail)
    /// </summary>
    public string? LastModifiedBy { get; set; }
}
