using BinTool.Application.Models.Common;

namespace BinTool.Application.Models.Commission;

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
