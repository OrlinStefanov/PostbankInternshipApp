using BinTool.Application.Models.Common;

namespace BinTool.Application.Models.ReferenceData;

public static class LookupMutationStatusExtensions
{
    public static MutationOutcome Outcome(this LookupMutationStatus status) => status switch
    {
        LookupMutationStatus.NotFound => MutationOutcome.NotFound,
        LookupMutationStatus.Invalid => MutationOutcome.Invalid,
        LookupMutationStatus.NameInUse => MutationOutcome.Conflict,
        LookupMutationStatus.AlreadyInThatState => MutationOutcome.Conflict,
        LookupMutationStatus.InUse => MutationOutcome.Conflict,
        _ => MutationOutcome.Succeeded
    };
}
