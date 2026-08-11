using BinTool.Domain.Entities;

namespace BinTool.Domain.Common;


public static class CommissionRuleExtensions
{
    public static DateRange Validity(this CommissionRule rule) =>
        DateRange.OfDays(rule.ValidFrom, rule.ValidTo);

    public static RuleCriteriaKey Key(this CommissionRule rule)
    {
        var criteria = rule.RuleCriteria.FirstOrDefault();
        return criteria is null ? default : RuleCriteriaKey.From(criteria);
    }

    public static int PriorityScore(this CommissionRule rule) =>
        rule.RuleCriteria.FirstOrDefault()?.PriorityScore ?? 0;

    public static bool AppliesOn(this CommissionRule rule, DateTime day) =>
        !rule.IsDeleted && rule.IsActive && rule.Validity().Contains(day.Date);

    public static bool IsScheduledOn(this CommissionRule rule, DateTime today) =>
        rule.Validity().StartsAfter(today.Date);

    public static bool HasExpiredOn(this CommissionRule rule, DateTime today) =>
        rule.Validity().EndedBefore(today.Date);
}
