using BinTool.Core.Models.ReferenceData;

namespace BinTool.Core.Models.Currency;

/// <summary>
/// The outcome of adding, editing, deleting or restoring one currency. Reuses the shared
/// <see cref="LookupMutationStatus"/> so the UI reads the same shape as the other reference
/// tables.
/// </summary>
public class CurrencyMutationResult
{
    public LookupMutationStatus Status { get; set; }

    public bool Succeeded =>
        Status is LookupMutationStatus.Created
            or LookupMutationStatus.Updated
            or LookupMutationStatus.Restored
            or LookupMutationStatus.Deleted;

    /// <summary>Why the write was refused. Null when it succeeded.</summary>
    public string? Error { get; set; }

    /// <summary>The currency as it now stands. Null when the write was refused.</summary>
    public CurrencyListItem? Item { get; set; }

    public static CurrencyMutationResult Success(LookupMutationStatus status, CurrencyListItem item) =>
        new() { Status = status, Item = item };

    public static CurrencyMutationResult Failure(LookupMutationStatus status, string error) =>
        new() { Status = status, Error = error };
}
