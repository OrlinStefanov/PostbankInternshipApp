namespace BinTool.Core.Entities;

/// <summary>
/// Bank Identification Number (BIN) range record
/// </summary>
public class BinRange
{
    public int BinRangeId { get; set; }

    /// <summary>
    /// BIN prefix (6 to 8 digits)
    /// </summary>
    public string Prefix { get; set; } = string.Empty;

    /// <summary>
    /// Length of the BIN prefix
    /// </summary>
    public int PrefixLength { get; set; }

    /// <summary>
    /// Card scheme for this BIN range
    /// </summary>
    public int CardSchemeId { get; set; }

    /// <summary>
    /// Product type for this BIN range
    /// </summary>
    public int ProductTypeId { get; set; }

    /// <summary>
    /// Funding type for this BIN range
    /// </summary>
    public int FundingTypeId { get; set; }

    /// <summary>
    /// Issuing country
    /// </summary>
    public int CountryId { get; set; }

    /// <summary>
    /// Valid from date (inclusive)
    /// </summary>
    public DateTime ValidFrom { get; set; }

    /// <summary>
    /// Valid until date (inclusive, nullable for open-ended)
    /// </summary>
    public DateTime? ValidTo { get; set; }

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

    public CardScheme? CardScheme { get; set; }

    public ProductType? ProductType { get; set; }

    public FundingType? FundingType { get; set; }

    public Country? Country { get; set; }

    #endregion
}
