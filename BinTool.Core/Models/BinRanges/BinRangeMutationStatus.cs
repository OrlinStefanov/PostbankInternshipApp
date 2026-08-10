namespace BinTool.Core.Models.BinRanges;

/// <summary>
/// What a single-range write actually did, or why it did nothing.
/// </summary>
public enum BinRangeMutationStatus
{
    /// <summary>A new range was inserted.</summary>
    Created = 0,

    /// <summary>An existing range was overwritten with the supplied values.</summary>
    Updated = 1,

    /// <summary>
    /// A previously deleted range came back. Either the caller restored it explicitly, or
    /// they added a prefix whose only record was soft-deleted - the prefix is unique across
    /// deleted rows too, so that row is revived in place rather than duplicated.
    /// </summary>
    Restored = 2,

    /// <summary>The range was soft-deleted. Nothing is erased.</summary>
    Deleted = 3,

    /// <summary>No range with that id exists, or it is deleted and must be restored first.</summary>
    NotFound = 4,

    /// <summary>Another range already owns the prefix.</summary>
    PrefixInUse = 5,

    /// <summary>The supplied values broke a validation rule, or named reference data that does not exist.</summary>
    Invalid = 6,

    /// <summary>Deleting an already-deleted range, or restoring one that was never deleted.</summary>
    AlreadyInThatState = 7,

    /// <summary>
    /// The BIN prefix belongs to a different card network than the caller declared, or
    /// to no network the detector knows. Refused unless the caller sets
    /// <see cref="BinRangeInput.AcknowledgeSchemeMismatch"/> - the detector is right often
    /// enough that a silent save would hide a data-entry mistake, but wrong often enough
    /// (co-branded ranges, new allocations) that the human has to have the last word.
    /// </summary>
    SchemeMismatch = 8
}
