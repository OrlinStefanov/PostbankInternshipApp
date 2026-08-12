using BinTool.Application.Models.Common;

namespace BinTool.Application.Models.ReferenceData;

public enum LookupMutationStatus
{
    /// <summary>A new row was inserted.</summary>
    Created = 0,

    /// <summary>An existing row was overwritten with the supplied values.</summary>
    Updated = 1,

    /// <summary>
    /// A soft-deleted row came back - either by explicit restore or by an add whose name/code was
    /// already held by a deleted row.
    /// </summary>
    Restored = 2,

    /// <summary>The row was soft-deleted.</summary>
    Deleted = 3,

    /// <summary>No row with that id exists, or it is deleted and must be restored first.</summary>
    NotFound = 4,

    /// <summary>Another row already owns the name (or ISO code, for Country).</summary>
    NameInUse = 5,

    /// <summary>
    /// The supplied values broke a validation rule or referenced data that does not exist.
    /// </summary>
    Invalid = 6,

    /// <summary>Deleting an already-deleted row, or restoring one that was never deleted.</summary>
    AlreadyInThatState = 7,

    /// <summary>
    /// Deleting a row that is still referenced by live BIN ranges (or by live countries, for a
    /// region).
    /// </summary>
    InUse = 8
}

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
