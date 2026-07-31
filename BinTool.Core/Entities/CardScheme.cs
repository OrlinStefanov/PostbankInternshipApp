namespace BinTool.Core.Entities;

/// <summary>
/// Card payment schemes (networks)
/// </summary>
public class CardScheme
{
    public int CardSchemeId { get; set; }

    /// <summary>
    /// Scheme name (e.g., "Visa", "Mastercard", "American Express")
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Description of the card scheme
    /// </summary>
    public string? Description { get; set; }

    #region Soft Delete

    public bool IsDeleted { get; set; }

    public DateTime? DeletedAt { get; set; }

    public string? DeletedBy { get; set; }

    #endregion

    #region Navigation Properties

    /// <summary>
    /// BIN ranges for this scheme
    /// </summary>
    public ICollection<BinRange> BinRanges { get; set; } = new List<BinRange>();

    /// <summary>
    /// Rule criteria using this scheme
    /// </summary>
    public ICollection<RuleCriteria> RuleCriteria { get; set; } = new List<RuleCriteria>();

    #endregion
}
