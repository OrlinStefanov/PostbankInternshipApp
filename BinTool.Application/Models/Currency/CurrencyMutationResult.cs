using System.Text.Json.Serialization;
using BinTool.Application.Models.Common;
using BinTool.Application.Models.ReferenceData;

namespace BinTool.Application.Models.Currency;

public class CurrencyMutationResult : IMutationResult
{
    public LookupMutationStatus Status { get; set; }

    // The status code says this.
    [JsonIgnore]
    public MutationOutcome Outcome => Status.Outcome();

    public bool Succeeded => Outcome == MutationOutcome.Succeeded;

    public string? Error { get; set; }

    public CurrencyListItem? Item { get; set; }

    public static CurrencyMutationResult Success(LookupMutationStatus status, CurrencyListItem item) =>
        new() { Status = status, Item = item };

    public static CurrencyMutationResult Failure(LookupMutationStatus status, string error) =>
        new() { Status = status, Error = error };
}
