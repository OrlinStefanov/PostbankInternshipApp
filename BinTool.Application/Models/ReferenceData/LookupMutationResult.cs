namespace BinTool.Application.Models.ReferenceData;

/// <summary>
/// The outcome of adding, editing, deleting or restoring one named reference row.
/// </summary>
public class LookupMutationResult
{
    public LookupMutationStatus Status { get; set; }

    public bool Succeeded =>
        Status is LookupMutationStatus.Created
            or LookupMutationStatus.Updated
            or LookupMutationStatus.Restored
            or LookupMutationStatus.Deleted;

    /// <summary>Why the write was refused. Null when it succeeded.</summary>
    public string? Error { get; set; }

    /// <summary>The row as it now stands. Null when the write was refused.</summary>
    public LookupListItem? Item { get; set; }

    public static LookupMutationResult Success(LookupMutationStatus status, LookupListItem item) =>
        new() { Status = status, Item = item };

    public static LookupMutationResult Failure(LookupMutationStatus status, string error) =>
        new() { Status = status, Error = error };
}
