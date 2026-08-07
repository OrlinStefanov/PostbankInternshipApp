namespace BinTool.Core.Models.Import;

/// <summary>
/// A staged conflict returned to the caller after an import. The prefix already
/// exists in the database with different values; <see cref="Differences"/> lists
/// exactly what would change so the user can decide whether to apply it.
/// </summary>
public class BinConflict
{
    /// <summary>
    /// Id of the persisted <c>PendingBinConflict</c>, used later to resolve it.
    /// </summary>
    public int PendingBinConflictId { get; set; }

    /// <summary>
    /// 1-based row number in the original CSV.
    /// </summary>
    public int RowNumber { get; set; }

    public string Prefix { get; set; } = string.Empty;

    /// <summary>
    /// Why the row was staged: <c>ValueConflict</c> (the prefix exists with different
    /// values) or <c>SchemeMismatch</c> (the declared scheme contradicts the prefix).
    /// A plain string so the API contract stays independent of the internal enum.
    /// </summary>
    public string ConflictType { get; set; } = "ValueConflict";

    /// <summary>
    /// A human explanation, set for a scheme mismatch (e.g. why the prefix and the
    /// declared scheme disagree). Null for a plain value conflict.
    /// </summary>
    public string? Message { get; set; }

    /// <summary>
    /// The fields that differ between the incoming row and the existing record. Empty
    /// for a scheme mismatch on a new prefix, where there is no existing row to diff.
    /// </summary>
    public List<BinFieldDiff> Differences { get; set; } = new();
}
