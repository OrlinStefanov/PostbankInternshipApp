using BinTool.Application.Models.Common;

namespace BinTool.Application.Models.BinRanges;

public static class BinRangeMutationStatusExtensions
{
    public static MutationOutcome Outcome(this BinRangeMutationStatus status) => status switch
    {
        BinRangeMutationStatus.NotFound => MutationOutcome.NotFound,
        BinRangeMutationStatus.Invalid => MutationOutcome.Invalid,
        BinRangeMutationStatus.PrefixInUse => MutationOutcome.Conflict,
        BinRangeMutationStatus.AlreadyInThatState => MutationOutcome.Conflict,
        BinRangeMutationStatus.SchemeMismatch => MutationOutcome.Conflict,
        _ => MutationOutcome.Succeeded
    };
}
