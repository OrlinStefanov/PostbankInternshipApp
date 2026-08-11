using BinTool.Domain.Entities;

namespace BinTool.Domain.Common;

/// <summary>
/// The lifecycle questions asked of a stored rule. Status is derived, never stored, so
/// every caller that asks has to derive it the same way or two screens will disagree about
/// whether a rule is live.
/// </summary>
public static class CommissionRuleExtensions
{
    /// <summary>The rule's validity window as a range, whole days.</summary>
    public static DateRange Validity(this CommissionRule rule) =>
        DateRange.OfDays(rule.ValidFrom, rule.ValidTo);

    /// <summary>The rule's key, or an all-wildcard key when it carries no criteria row.</summary>
    public static RuleCriteriaKey Key(this CommissionRule rule)
    {
        var criteria = rule.RuleCriteria.FirstOrDefault();
        return criteria is null ? default : RuleCriteriaKey.From(criteria);
    }

    /// <summary>The stored score, or zero when the rule carries no criteria row.</summary>
    public static int PriorityScore(this CommissionRule rule) =>
        rule.RuleCriteria.FirstOrDefault()?.PriorityScore ?? 0;

    /// <summary>True when the rule takes part in resolution on the given day.</summary>
    public static bool AppliesOn(this CommissionRule rule, DateTime day) =>
        !rule.IsDeleted && rule.IsActive && rule.Validity().Contains(day.Date);

    /// <summary>True when the rule is live but has not started yet.</summary>
    public static bool IsScheduledOn(this CommissionRule rule, DateTime today) =>
        rule.Validity().StartsAfter(today.Date);

    /// <summary>True when the rule's window has already closed.</summary>
    public static bool HasExpiredOn(this CommissionRule rule, DateTime today) =>
        rule.Validity().EndedBefore(today.Date);
}
