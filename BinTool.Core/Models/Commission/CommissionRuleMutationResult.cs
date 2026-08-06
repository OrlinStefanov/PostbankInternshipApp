namespace BinTool.Core.Models.Commission;

/// <summary>
/// The outcome of adding, editing, deleting, restoring or defaulting one commission rule.
/// <para>
/// On an <see cref="CommissionRuleMutationStatus.Overlap"/> refusal the conflicting rule is
/// named, so the caller can point the user straight at what they collided with.
/// </para>
/// </summary>
public class CommissionRuleMutationResult
{
    public CommissionRuleMutationStatus Status { get; set; }

    /// <summary>True when the database changed.</summary>
    public bool Succeeded =>
        Status is CommissionRuleMutationStatus.Created
            or CommissionRuleMutationStatus.Updated
            or CommissionRuleMutationStatus.Deleted
            or CommissionRuleMutationStatus.Restored;

    /// <summary>Why the write was refused. Null when it succeeded.</summary>
    public string? Error { get; set; }

    /// <summary>The rule as it now stands. Null when the write was refused.</summary>
    public CommissionRuleListItem? Rule { get; set; }

    /// <summary>On an overlap, the id of the rule that already covers the window.</summary>
    public int? ConflictingRuleId { get; set; }

    /// <summary>On an overlap, the name of the rule that already covers the window.</summary>
    public string? ConflictingRuleName { get; set; }

    public static CommissionRuleMutationResult Success(
        CommissionRuleMutationStatus status, CommissionRuleListItem rule) =>
        new() { Status = status, Rule = rule };

    public static CommissionRuleMutationResult Failure(
        CommissionRuleMutationStatus status, string error) =>
        new() { Status = status, Error = error };

    public static CommissionRuleMutationResult Conflict(
        string error, int conflictingRuleId, string conflictingRuleName) =>
        new()
        {
            Status = CommissionRuleMutationStatus.Overlap,
            Error = error,
            ConflictingRuleId = conflictingRuleId,
            ConflictingRuleName = conflictingRuleName
        };
}
