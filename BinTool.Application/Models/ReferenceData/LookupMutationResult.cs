using System.Text.Json.Serialization;
using BinTool.Application.Models.Common;
namespace BinTool.Application.Models.ReferenceData;

public class LookupMutationResult : IMutationResult
{
    public LookupMutationStatus Status { get; set; }

    // The status code says this.
    [JsonIgnore]
    public MutationOutcome Outcome => Status.Outcome();

    public bool Succeeded => Outcome == MutationOutcome.Succeeded;

    public string? Error { get; set; }

    public LookupListItem? Item { get; set; }

    public static LookupMutationResult Success(LookupMutationStatus status, LookupListItem item) =>
        new() { Status = status, Item = item };

    public static LookupMutationResult Failure(LookupMutationStatus status, string error) =>
        new() { Status = status, Error = error };
}
