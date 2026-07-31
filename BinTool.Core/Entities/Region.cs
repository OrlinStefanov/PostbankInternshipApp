namespace BinTool.Core.Entities;

/// <summary>
/// Geographic region for determining fee rules and regulations
/// </summary>
public class Region
{
    public int Id { get; set; }

    /// <summary>
    /// Region type (Domestic, IntraEEA, InterRegional, Unknown)
    /// </summary>
    public RegionType Type { get; set; }

    /// <summary>
    /// Display name for the region
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Description of the region's characteristics and fee implications
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Navigation property for countries in this region
    /// </summary>
    public ICollection<Country> Countries { get; set; } = new List<Country>();

    /// <summary>
    /// Navigation property for commission rules applicable to this region
    /// </summary>
    public ICollection<CommissionRule> CommissionRules { get; set; } = new List<CommissionRule>();
}
