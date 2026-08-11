namespace BinTool.Domain.Common;

/// <summary>
/// The bounds an admin's ranking numbers have to stay inside. Constants rather than
/// literals because the same two numbers appear in the input model's validation attributes,
/// in the editor's min/max, and in the message a caller sees when they overshoot - and those
/// three disagreeing is exactly the sort of thing nobody notices until a save is refused
/// with a range the form allowed.
/// </summary>
public static class CommissionRuleLimits
{
    public const int MinPriority = 0;
    public const int MaxPriority = 100;

    /// <summary>
    /// The score ceiling is deliberately far above the 0-4 the system suggests, so an
    /// override has room to sit above every suggested score rather than tying with one.
    /// </summary>
    public const int MinPriorityScore = 0;
    public const int MaxPriorityScore = 100;
}
