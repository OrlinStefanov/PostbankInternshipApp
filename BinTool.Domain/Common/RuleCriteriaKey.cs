using BinTool.Domain.Entities;

namespace BinTool.Domain.Common;

/// <summary>
/// The four attributes a commission rule is keyed on. A null field is a wildcard, so
/// <c>(Visa, null, null, null)</c> is "any Visa card".
/// </summary>
public readonly record struct RuleCriteriaKey(
    int? CardSchemeId, int? ProductTypeId, int? FundingTypeId, int? RegionId)
{
    /// <summary>How many fields the key has; the ceiling for a suggested score.</summary>
    public const int FieldCount = 4;

    public static RuleCriteriaKey From(RuleCriteria criteria) =>
        new(criteria.CardSchemeId, criteria.ProductTypeId,
            criteria.FundingTypeId, criteria.RegionId);

    /// <summary>
    /// The score the system offers when the admin does not set one: the number of fields
    /// that are not wildcards, so a narrower rule starts out ranked above a broader one.
    /// It is only a starting point - the stored score is the admin's to overrule.
    /// </summary>
    public int SuggestedPriorityScore =>
        (CardSchemeId is null ? 0 : 1)
        + (ProductTypeId is null ? 0 : 1)
        + (FundingTypeId is null ? 0 : 1)
        + (RegionId is null ? 0 : 1);

    /// <summary>True when every field is a wildcard, so the key matches every card.</summary>
    public bool IsWildcard => SuggestedPriorityScore == 0;

    /// <summary>
    /// True when some real card would match both keys - that is, for every field, either
    /// side is a wildcard or the two ids agree.
    /// <para>
    /// This is weaker than equality and that is the point: <c>(Visa, Consumer, *, *)</c> and
    /// <c>(Visa, *, Credit, *)</c> are different keys, yet a Visa consumer credit card matches
    /// both. Two such rules cannot be allowed to share a priority and a score, or which one
    /// prices a transaction comes down to row id.
    /// </para>
    /// </summary>
    public bool IsCoMatchableWith(RuleCriteriaKey other) =>
        FieldsAgree(CardSchemeId, other.CardSchemeId)
        && FieldsAgree(ProductTypeId, other.ProductTypeId)
        && FieldsAgree(FundingTypeId, other.FundingTypeId)
        && FieldsAgree(RegionId, other.RegionId);

    /// <summary>True when this key matches a card with the given attributes.</summary>
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
