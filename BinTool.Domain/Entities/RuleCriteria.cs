namespace BinTool.Domain.Entities;

/// <summary>
/// Rule criteria that defines which card attributes a commission rule applies to
/// Supports nullable fields for wildcard matching (any scheme, any product, any region)
/// </summary>
public class RuleCriteria
{
    public int RuleCriteriaId { get; set; }

    /// <summary>
    /// Commission rule this criteria belongs to
    /// </summary>
    public int CommissionRuleId { get; set; }

    /// <summary>
    /// Card scheme this rule applies to (nullable = wildcard/any scheme)
    /// </summary>
    public int? CardSchemeId { get; set; }

    /// <summary>
    /// Product type this rule applies to (nullable = wildcard/any product)
    /// </summary>
    public int? ProductTypeId { get; set; }

    /// <summary>
    /// Funding type this rule applies to (nullable = wildcard/any funding)
    /// </summary>
    public int? FundingTypeId { get; set; }

    /// <summary>
    /// Region this rule applies to (nullable = wildcard/any region)
    /// </summary>
    public int? RegionId { get; set; }

    /// <summary>
    /// Priority score for rule resolution (higher = more specific)
    /// Calculated based on number of non-null criteria fields
    /// </summary>
    public int PriorityScore { get; set; }

    #region Navigation Properties

    public CommissionRule? CommissionRule { get; set; }

    public CardScheme? CardScheme { get; set; }

    public ProductType? ProductType { get; set; }

    public FundingType? FundingType { get; set; }

    public Region? Region { get; set; }

    #endregion
}
