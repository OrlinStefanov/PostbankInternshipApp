namespace BinTool.Core.Models.BinRanges;

/// <summary>
/// The outcome of adding, editing, deleting or restoring one BIN range.
/// <para>
/// A refusal is reported rather than thrown: "that prefix already exists" is an ordinary
/// answer to an ordinary request, and the caller needs the reason to show the user.
/// </para>
/// </summary>
public class BinRangeMutationResult
{
    public BinRangeMutationStatus Status { get; set; }

    /// <summary>
    /// True when the database changed.
    /// </summary>
    public bool Succeeded =>
        Status is BinRangeMutationStatus.Created
            or BinRangeMutationStatus.Updated
            or BinRangeMutationStatus.Restored
            or BinRangeMutationStatus.Deleted;

    /// <summary>
    /// Why the write was refused. Null when it succeeded.
    /// </summary>
    public string? Error { get; set; }

    /// <summary>
    /// The range as it now stands, in the same shape the browse listing returns.
    /// Null when the write was refused.
    /// </summary>
    public BinRangeListItem? Range { get; set; }

    public static BinRangeMutationResult Success(
        BinRangeMutationStatus status, BinRangeListItem range) =>
        new() { Status = status, Range = range };

    public static BinRangeMutationResult Failure(BinRangeMutationStatus status, string error) =>
        new() { Status = status, Error = error };
}
