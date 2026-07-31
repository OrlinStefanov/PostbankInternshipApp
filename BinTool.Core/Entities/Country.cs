namespace BinTool.Core.Entities;

/// <summary>
/// Country information with region assignment
/// </summary>
public class Country
{
    public int Id { get; set; }

    /// <summary>
    /// ISO 3166-1 alpha-2 country code (e.g., "US", "BG", "DE")
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Full country name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Region this country belongs to
    /// </summary>
    public int RegionId { get; set; }

    /// <summary>
    /// Navigation property for the region
    /// </summary>
    public Region? Region { get; set; }

    /// <summary>
    /// Timestamp when this record was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Timestamp when this record was last updated
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Navigation property for BIN ranges from this country
    /// </summary>
    public ICollection<BinRange> BinRanges { get; set; } = new List<BinRange>();
}
