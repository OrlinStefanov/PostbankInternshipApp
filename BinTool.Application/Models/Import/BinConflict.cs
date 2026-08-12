namespace BinTool.Application.Models.Import;

public class BinConflict
{
    /// <summary>Id of the persisted <c>PendingBinConflict</c>, used later to resolve it.</summary>
    public int PendingBinConflictId { get; set; }

    /// <summary>1-based row number in the original CSV.</summary>
    public int RowNumber { get; set; }

    public string Prefix { get; set; } = string.Empty;

    /// <summary>
    /// Why the row was staged: <c>ValueConflict</c> (the prefix exists with different values) or
    /// <c>SchemeMismatch</c> (the declared scheme contradicts the prefix).
    /// </summary>
    public string ConflictType { get; set; } = "ValueConflict";

    /// <summary>
    /// A human explanation, set for a scheme mismatch (e.g. why the prefix and the declared scheme
    /// disagree).
    /// </summary>
    public string? Message { get; set; }

    /// <summary>The fields that differ between the incoming row and the existing record.</summary>
    public List<BinFieldDiff> Differences { get; set; } = new();

    /// <summary>
    /// The card scheme the detector would assign to this prefix, or null if the prefix sits in no
    /// range the detector knows.
    /// </summary>
    public string? DetectedScheme { get; set; }

    /// <summary>
    /// Set when the stored row's scheme contradicts the detector's opinion of the prefix - i.e. the
    /// database already holds the wrong network for this BIN and the incoming row would leave it
    /// that way.
    /// </summary>
    public string? SchemeAdvisory { get; set; }
}
