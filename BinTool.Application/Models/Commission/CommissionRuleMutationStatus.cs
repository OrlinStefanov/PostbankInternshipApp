using BinTool.Application.Models.Common;

namespace BinTool.Application.Models.Commission;

/// <summary>
/// The outcome of one write to a commission rule. Refusals are values, not exceptions -
/// "that overlaps an existing rule" is an ordinary answer the caller shows to the user.
/// </summary>
public enum CommissionRuleMutationStatus
{
    Created = 0,
    Updated = 1,
    Deleted = 2,
    Restored = 3,

    /// <summary>No rule has that id.</summary>
    NotFound = 4,

    /// <summary>A field broke a validation rule (negative rate, ValidTo before ValidFrom, …).</summary>
    Invalid = 5,

    /// <summary>
    /// The row is still referenced by live data and cannot be deleted - a rule that is the
    /// configured default must be replaced before it can go.
    /// </summary>
    InUse = 6,

    /// <summary>A delete of an already-deleted rule, or a restore of a live one.</summary>
    AlreadyInThatState = 7,

    /// <summary>
    /// Another rule with the same scheme/product/region key already covers part of this
    /// rule's validity window. The result names the conflicting rule.
    /// </summary>
    Overlap = 8
}

public static class CommissionRuleMutationStatusExtensions
{
    public static MutationOutcome Outcome(this CommissionRuleMutationStatus status) => status switch
    {
        CommissionRuleMutationStatus.NotFound => MutationOutcome.NotFound,
        CommissionRuleMutationStatus.Invalid => MutationOutcome.Invalid,
        CommissionRuleMutationStatus.Overlap => MutationOutcome.Conflict,
        CommissionRuleMutationStatus.InUse => MutationOutcome.Conflict,
        CommissionRuleMutationStatus.AlreadyInThatState => MutationOutcome.Conflict,
        _ => MutationOutcome.Succeeded
    };
}
