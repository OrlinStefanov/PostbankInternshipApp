namespace BinTool.Domain.Entities;

/// <summary>
/// Country information with region assignment
/// </summary>
public class Country
{
    public int CountryId { get; set; }

    /// <summary>
    /// ISO 3166-1 alpha-2 country code (e.g., "US", "BG", "DE")
    /// </summary>
    public string IsoCode { get; set; } = string.Empty;

    /// <summary>
    /// Full country name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Region this country belongs to
    /// </summary>
    public int RegionId { get; set; }

    #region Audit Fields

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public string? CreatedBy { get; set; }

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public string? UpdatedBy { get; set; }

    #endregion

    #region Soft Delete

    public bool IsDeleted { get; set; }

    public DateTime? DeletedAt { get; set; }

    public string? DeletedBy { get; set; }

    #endregion

    #region Navigation Properties

    /// <summary>
    /// Navigation property for the region
    /// </summary>
    public Region? Region { get; set; }

    /// <summary>
    /// BIN ranges from this country
    /// </summary>
    public ICollection<BinRange> BinRanges { get; set; } = new List<BinRange>();

    #endregion
}
