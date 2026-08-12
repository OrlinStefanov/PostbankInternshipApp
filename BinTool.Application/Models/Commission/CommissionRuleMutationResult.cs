using System.Text.Json.Serialization;
using BinTool.Application.Models.Common;
namespace BinTool.Application.Models.Commission;

public class CommissionRuleMutationResult : IMutationResult
{
    public CommissionRuleMutationStatus Status { get; set; }

    // The status code carries this, so it is not repeated in the body.
    [JsonIgnore]
    public MutationOutcome Outcome => Status.Outcome();

    /// <summary>True when the database changed.</summary>
    public bool Succeeded => Outcome == MutationOutcome.Succeeded;

    /// <summary>Why the write was refused.</summary>
    public string? Error { get; set; }

    /// <summary>The rule as it now stands.</summary>
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
