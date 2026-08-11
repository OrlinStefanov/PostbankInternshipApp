namespace BinTool.Domain.Entities;

public class RuleCriteria
{
    public int RuleCriteriaId { get; set; }

    public int CommissionRuleId { get; set; }

    public int? CardSchemeId { get; set; }

    public int? ProductTypeId { get; set; }

    public int? FundingTypeId { get; set; }

    public int? RegionId { get; set; }

    public int PriorityScore { get; set; }

    #region Navigation Properties

    public CommissionRule? CommissionRule { get; set; }

    public CardScheme? CardScheme { get; set; }

    public ProductType? ProductType { get; set; }

    public FundingType? FundingType { get; set; }

    public Region? Region { get; set; }

    #endregion
}
