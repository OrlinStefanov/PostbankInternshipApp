using BinTool.Application.Models.Common;

namespace BinTool.Application.Models.Access;

public static class RoleMutationStatusExtensions
{
    public static MutationOutcome Outcome(this RoleMutationStatus status) => status switch
    {
        RoleMutationStatus.NotFound => MutationOutcome.NotFound,
        RoleMutationStatus.Invalid => MutationOutcome.Invalid,
        RoleMutationStatus.NameInUse => MutationOutcome.Conflict,
        RoleMutationStatus.Protected => MutationOutcome.Conflict,
        RoleMutationStatus.InUse => MutationOutcome.Conflict,
        _ => MutationOutcome.Succeeded
    };
}
