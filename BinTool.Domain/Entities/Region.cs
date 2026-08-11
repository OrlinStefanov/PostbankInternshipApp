namespace BinTool.Domain.Entities;

/// <summary>
/// Geographic region for determining fee rules and regulations
/// </summary>
public class Region
{
    public int RegionId { get; set; }

    /// <summary>
    /// Region name (e.g., "Domestic", "Intra-EEA", "Inter-Regional")
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Description of the region's characteristics and fee implications
    /// </summary>
    public string? Description { get; set; }

    #region Soft Delete

    public bool IsDeleted { get; set; }

    public DateTime? DeletedAt { get; set; }

    public string? DeletedBy { get; set; }

    #endregion

    #region Navigation Properties

    /// <summary>
    /// Countries in this region
    /// </summary>
    public ICollection<Country> Countries { get; set; } = new List<Country>();

    /// <summary>
    /// Commission rules applicable to this region
    /// </summary>
    public ICollection<RuleCriteria> RuleCriteria { get; set; } = new List<RuleCriteria>();

    #endregion
}
