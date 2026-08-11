namespace BinTool.Application.Models.ReferenceData;

/// <summary>
/// The outcome of adding, editing, deleting or restoring one country. Shares
/// <see cref="LookupMutationStatus"/> with the named lookups so clients read one shape;
/// the <c>NameInUse</c> value covers a duplicate ISO code as well as a duplicate name.
/// </summary>
public class CountryMutationResult
{
    public LookupMutationStatus Status { get; set; }

    public bool Succeeded =>
        Status is LookupMutationStatus.Created
            or LookupMutationStatus.Updated
            or LookupMutationStatus.Restored
            or LookupMutationStatus.Deleted;

    public string? Error { get; set; }

    public CountryListItem? Country { get; set; }

    public static CountryMutationResult Success(LookupMutationStatus status, CountryListItem country) =>
        new() { Status = status, Country = country };

    public static CountryMutationResult Failure(LookupMutationStatus status, string error) =>
        new() { Status = status, Error = error };
}
