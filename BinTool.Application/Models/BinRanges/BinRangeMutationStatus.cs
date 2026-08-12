using BinTool.Application.Models.Common;

namespace BinTool.Application.Models.BinRanges;

public enum BinRangeMutationStatus
{
    /// <summary>A new range was inserted.</summary>
    Created = 0,

    /// <summary>An existing range was overwritten with the supplied values.</summary>
    Updated = 1,

    /// <summary>A previously deleted range came back.</summary>
    Restored = 2,

    /// <summary>The range was soft-deleted.</summary>
    Deleted = 3,

    /// <summary>
    /// No range with that id exists, or it is deleted and must be restored first.
    /// </summary>
    NotFound = 4,

    /// <summary>Another range already owns the prefix.</summary>
    PrefixInUse = 5,

    /// <summary>
    /// The supplied values broke a validation rule, or named reference data that does not exist.
    /// </summary>
    Invalid = 6,

    /// <summary>
    /// Deleting an already-deleted range, or restoring one that was never deleted.
    /// </summary>
    AlreadyInThatState = 7,

    /// <summary>
    /// The BIN prefix belongs to a different card network than the caller declared, or to no
    /// network the detector knows.
    /// </summary>
    SchemeMismatch = 8
}

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
