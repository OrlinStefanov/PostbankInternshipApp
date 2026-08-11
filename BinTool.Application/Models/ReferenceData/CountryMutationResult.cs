using System.Text.Json.Serialization;
using BinTool.Application.Models.Common;
namespace BinTool.Application.Models.ReferenceData;

/// <summary>
/// The outcome of adding, editing, deleting or restoring one country. Shares
/// <see cref="LookupMutationStatus"/> with the named lookups so clients read one shape;
/// the <c>NameInUse</c> value covers a duplicate ISO code as well as a duplicate name.
/// </summary>
public class CountryMutationResult : IMutationResult
{
    public LookupMutationStatus Status { get; set; }

    // The status code carries this, so it is not repeated in the body.
    [JsonIgnore]
    public MutationOutcome Outcome => Status.Outcome();

    public bool Succeeded => Outcome == MutationOutcome.Succeeded;

    public string? Error { get; set; }

    public CountryListItem? Country { get; set; }

    public static CountryMutationResult Success(LookupMutationStatus status, CountryListItem country) =>
        new() { Status = status, Country = country };

    public static CountryMutationResult Failure(LookupMutationStatus status, string error) =>
        new() { Status = status, Error = error };
}
