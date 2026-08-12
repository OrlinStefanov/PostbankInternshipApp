using System.Text.Json.Serialization;
using BinTool.Application.Models.Common;
namespace BinTool.Application.Models.BinRanges;

public class BinRangeMutationResult : IMutationResult
{
    public BinRangeMutationStatus Status { get; set; }

    // The status code carries this, so it is not repeated in the body.
    [JsonIgnore]
    public MutationOutcome Outcome => Status.Outcome();

    /// <summary>True when the database changed.</summary>
    public bool Succeeded => Outcome == MutationOutcome.Succeeded;

    /// <summary>Why the write was refused.</summary>
    public string? Error { get; set; }

    /// <summary>The range as it now stands, in the same shape the browse listing returns.</summary>
    public BinRangeListItem? Range { get; set; }

    public static BinRangeMutationResult Success(
        BinRangeMutationStatus status, BinRangeListItem range) =>
        new() { Status = status, Range = range };

    public static BinRangeMutationResult Failure(BinRangeMutationStatus status, string error) =>
        new() { Status = status, Error = error };
}
