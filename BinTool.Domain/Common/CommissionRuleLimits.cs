namespace BinTool.Domain.Common;

public static class CommissionRuleLimits
{
    public const int MinPriority = 0;
    public const int MaxPriority = 100;

    // The score is a tie-breaker suggested as the number of non-wildcard key fields, of
    // which there are four (scheme, product, funding, region), so four is its ceiling.
    public const int MinPriorityScore = 0;
    public const int MaxPriorityScore = 4;
}
