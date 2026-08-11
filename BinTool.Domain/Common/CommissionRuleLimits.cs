namespace BinTool.Domain.Common;

/// <summary>
/// The bounds an admin's ranking numbers have to stay inside.
/// </summary>
public static class CommissionRuleLimits
{
    public const int MinPriority = 0;
    public const int MaxPriority = 100;

    public const int MinPriorityScore = 0;
    public const int MaxPriorityScore = 100;
}
