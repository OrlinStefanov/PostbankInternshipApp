using System.Text.Json.Serialization;
using BinTool.Application.Models.Common;
namespace BinTool.Application.Models.BinRanges;

/// <summary>
/// The outcome of adding, editing, deleting or restoring one BIN range.
/// <para>
/// A refusal is reported rather than thrown: "that prefix already exists" is an ordinary
/// answer to an ordinary request, and the caller needs the reason to show the user.
/// </para>
/// </summary>
public class BinRangeMutationResult : IMutationResult
{
    public BinRangeMutationStatus Status { get; set; }

    // The status code carries this, so it is not repeated in the body.
    [JsonIgnore]
    public MutationOutcome Outcome => Status.Outcome();

    /// <summary>True when the database changed.</summary>
    public bool Succeeded => Outcome == MutationOutcome.Succeeded;

    /// <summary>Why the write was refused. Null when it succeeded.</summary>
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
