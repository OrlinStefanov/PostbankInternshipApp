using System.Text.Json.Serialization;
using BinTool.Application.Models.Common;
namespace BinTool.Application.Models.BinRanges;

public class BinRangeMutationResult : IMutationResult
{
    public BinRangeMutationStatus Status { get; set; }

    // The status code says this.
    [JsonIgnore]
    public MutationOutcome Outcome => Status.Outcome();

    public bool Succeeded => Outcome == MutationOutcome.Succeeded;

    public string? Error { get; set; }

    public BinRangeListItem? Range { get; set; }

    public static BinRangeMutationResult Success(
        BinRangeMutationStatus status, BinRangeListItem range) =>
        new() { Status = status, Range = range };

    public static BinRangeMutationResult Failure(BinRangeMutationStatus status, string error) =>
        new() { Status = status, Error = error };
}
