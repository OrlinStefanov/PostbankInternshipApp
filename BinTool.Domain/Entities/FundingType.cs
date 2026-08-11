namespace BinTool.Domain.Entities;

/// <summary>
/// Card funding types (Credit, Debit, etc.)
/// </summary>
public class FundingType
{
    public int FundingTypeId { get; set; }

    /// <summary>
    /// Funding type name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Description of the funding type
    /// </summary>
    public string? Description { get; set; }

    #region Soft Delete

    public bool IsDeleted { get; set; }

    public DateTime? DeletedAt { get; set; }

    public string? DeletedBy { get; set; }

    #endregion

    #region Navigation Properties

    /// <summary>
    /// BIN ranges with this funding type
    /// </summary>
    public ICollection<BinRange> BinRanges { get; set; } = new List<BinRange>();

    /// <summary>
    /// Rule criteria using this funding type
    /// </summary>
    public ICollection<RuleCriteria> RuleCriteria { get; set; } = new List<RuleCriteria>();

    #endregion
}
