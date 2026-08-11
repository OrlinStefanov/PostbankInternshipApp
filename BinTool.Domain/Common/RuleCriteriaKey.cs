using BinTool.Domain.Entities;

namespace BinTool.Domain.Common;

public readonly record struct RuleCriteriaKey(
    int? CardSchemeId, int? ProductTypeId, int? FundingTypeId, int? RegionId)
{
    public const int FieldCount = 4;

    public static RuleCriteriaKey From(RuleCriteria criteria) =>
        new(criteria.CardSchemeId, criteria.ProductTypeId,
            criteria.FundingTypeId, criteria.RegionId);

    public int SuggestedPriorityScore =>
        (CardSchemeId is null ? 0 : 1)
        + (ProductTypeId is null ? 0 : 1)
        + (FundingTypeId is null ? 0 : 1)
        + (RegionId is null ? 0 : 1);

    public bool IsWildcard => SuggestedPriorityScore == 0;

    public bool IsCoMatchableWith(RuleCriteriaKey other) =>
        FieldsAgree(CardSchemeId, other.CardSchemeId)
        && FieldsAgree(ProductTypeId, other.ProductTypeId)
        && FieldsAgree(FundingTypeId, other.FundingTypeId)
        && FieldsAgree(RegionId, other.RegionId);

    public bool Matches(int cardSchemeId, int productTypeId, int fundingTypeId, int regionId) =>
        FieldMatches(CardSchemeId, cardSchemeId)
        && FieldMatches(ProductTypeId, productTypeId)
        && FieldMatches(FundingTypeId, fundingTypeId)
        && FieldMatches(RegionId, regionId);

    private static bool FieldsAgree(int? left, int? right) =>
        left is null || right is null || left == right;

    private static bool FieldMatches(int? criteria, int actual) =>
        criteria is null || criteria == actual;
}
