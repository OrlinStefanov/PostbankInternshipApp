namespace BinTool.Core.Entities;

/// <summary>
/// Card product types (Consumer, Commercial, Prepaid)
/// </summary>
public class ProductType
{
    public int ProductTypeId { get; set; }

    /// <summary>
    /// Product type name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Description of the product type
    /// </summary>
    public string? Description { get; set; }

    #region Soft Delete

    public bool IsDeleted { get; set; }

    public DateTime? DeletedAt { get; set; }

    public string? DeletedBy { get; set; }

    #endregion

    #region Navigation Properties

    /// <summary>
    /// BIN ranges with this product type
    /// </summary>
    public ICollection<BinRange> BinRanges { get; set; } = new List<BinRange>();

    /// <summary>
    /// Rule criteria using this product type
    /// </summary>
    public ICollection<RuleCriteria> RuleCriteria { get; set; } = new List<RuleCriteria>();

    #endregion
}
