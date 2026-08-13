using BinTool.Application.Models.Audit;
using BinTool.Application.Models.Commission;
using BinTool.Application.Models.ReferenceData;
using BinTool.Domain.Common;

namespace BinTool.Application.Mapping;

public static class CommissionRuleMapper
{
    public static RuleCriteriaKey Key(this CommissionRuleInput input) =>
        new(input.CardSchemeId, input.ProductTypeId, input.FundingTypeId, input.RegionId);

    public static DateRange Validity(this CommissionRuleInput input) =>
        DateRange.OfDays(input.ValidFrom, input.ValidTo);

    public static int EffectivePriorityScore(this CommissionRuleInput input) =>
        input.PriorityScore ?? input.Key().SuggestedPriorityScore;

    public static CommissionRuleListItem ToListItem(
        CommissionRule rule, int? defaultRuleId, DateTime today)
    {
        var criteria = rule.RuleCriteria.
            OrderBy(r => r.PriorityScore)
            .FirstOrDefault();

        return new CommissionRuleListItem
        {
            Id = rule.CommissionRuleId,
            RuleName = rule.RuleName,
            CardSchemeId = criteria?.CardSchemeId,
            CardSchemeName = criteria?.CardScheme?.Name,
            ProductTypeId = criteria?.ProductTypeId,
            ProductTypeName = criteria?.ProductType?.Name,
            FundingTypeId = criteria?.FundingTypeId,
            FundingTypeName = criteria?.FundingType?.Name,
            RegionId = criteria?.RegionId,
            RegionName = criteria?.Region?.Name,
            CurrencyId = rule.CurrencyId,
            CurrencyCode = rule.Currency?.Code ?? string.Empty,
            PercentageRate = rule.PercentageRate,
            FixedAmount = rule.FixedAmount,
            MinimumFee = rule.MinimumFee,
            Priority = rule.Priority,
            PriorityScore = rule.PriorityScore(),
            Specificity = rule.Key().SuggestedPriorityScore,
            ValidFrom = rule.ValidFrom,
            ValidTo = rule.ValidTo,
            IsActive = rule.IsActive,
            Status = StatusOf(rule, today),
            IsDefault = defaultRuleId == rule.CommissionRuleId,
            CreatedAt = rule.CreatedAt,
            CreatedBy = rule.CreatedBy,
            UpdatedAt = rule.UpdatedAt,
            UpdatedBy = rule.UpdatedBy,
            DeletedAt = rule.DeletedAt,
            DeletedBy = rule.DeletedBy
        };
    }

    public static CommissionRuleStatus StatusOf(CommissionRule rule, DateTime today)
    {
        if (rule.IsDeleted) return CommissionRuleStatus.Deleted;
        if (!rule.IsActive) return CommissionRuleStatus.Inactive;
        if (rule.IsScheduledOn(today)) return CommissionRuleStatus.Scheduled;
        if (rule.HasExpiredOn(today)) return CommissionRuleStatus.Expired;

        return CommissionRuleStatus.Active;
    }

    public static CommissionRuleSnapshot ToSnapshot(CommissionRule rule)
    {
        var criteria = rule.RuleCriteria.FirstOrDefault();

        return new CommissionRuleSnapshot(
            rule.RuleName,
            criteria?.CardScheme?.Name ?? CommissionRuleSnapshot.AnyValue,
            criteria?.ProductType?.Name ?? CommissionRuleSnapshot.AnyValue,
            criteria?.FundingType?.Name ?? CommissionRuleSnapshot.AnyValue,
            criteria?.Region?.Name ?? CommissionRuleSnapshot.AnyValue,
            rule.Currency?.Code ?? string.Empty,
            rule.PercentageRate,
            rule.FixedAmount,
            rule.MinimumFee,
            rule.Priority,
            rule.PriorityScore(),
            CommissionRuleSnapshot.Date(rule.ValidFrom),
            rule.ValidTo is null ? null : CommissionRuleSnapshot.Date(rule.ValidTo.Value),
            rule.IsActive,
            rule.IsDeleted);
    }

    public static CommissionRuleSnapshot ToSnapshot(
        CommissionRuleInput input, ReferenceNames names, bool isDeleted) =>
        new(input.RuleName.Trim(),
            names.CardScheme ?? CommissionRuleSnapshot.AnyValue,
            names.ProductType ?? CommissionRuleSnapshot.AnyValue,
            names.FundingType ?? CommissionRuleSnapshot.AnyValue,
            names.Region ?? CommissionRuleSnapshot.AnyValue,
            names.Currency ?? string.Empty,
            input.PercentageRate,
            input.FixedAmount,
            input.MinimumFee,
            input.Priority,
            input.EffectivePriorityScore(),
            CommissionRuleSnapshot.Date(input.ValidFrom.Date),
            input.ValidTo is null ? null : CommissionRuleSnapshot.Date(input.ValidTo.Value.Date),
            input.IsActive,
            isDeleted);

    public static void Apply(
        CommissionRule rule, CommissionRuleInput input, string user, DateTime now)
    {
        rule.RuleName = input.RuleName.Trim();
        rule.CurrencyId = input.CurrencyId;
        rule.PercentageRate = input.PercentageRate;
        rule.FixedAmount = input.FixedAmount;
        rule.MinimumFee = input.MinimumFee;
        rule.Priority = input.Priority;
        rule.ValidFrom = input.ValidFrom.Date;
        rule.ValidTo = input.ValidTo?.Date;
        rule.IsActive = input.IsActive;
        rule.UpdatedAt = now;
        rule.UpdatedBy = user;
    }

    public static void ApplyCriteria(RuleCriteria criteria, CommissionRuleInput input)
    {
        criteria.CardSchemeId = input.CardSchemeId;
        criteria.ProductTypeId = input.ProductTypeId;
        criteria.FundingTypeId = input.FundingTypeId;
        criteria.RegionId = input.RegionId;
        criteria.PriorityScore = input.EffectivePriorityScore();
    }
}
