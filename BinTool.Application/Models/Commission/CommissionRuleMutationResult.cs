using System.Text.Json.Serialization;
using BinTool.Application.Models.Common;
namespace BinTool.Application.Models.Commission;

public class CommissionRuleMutationResult : IMutationResult
{
    public CommissionRuleMutationStatus Status { get; set; }

    // The status code says this.
    [JsonIgnore]
    public MutationOutcome Outcome => Status.Outcome();

    public bool Succeeded => Outcome == MutationOutcome.Succeeded;

    public string? Error { get; set; }

    public CommissionRuleListItem? Rule { get; set; }

    public int? ConflictingRuleId { get; set; }

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
