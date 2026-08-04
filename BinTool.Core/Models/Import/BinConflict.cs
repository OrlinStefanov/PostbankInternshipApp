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
    /// The fields that differ between the incoming row and the existing record.
    /// </summary>
    public List<BinFieldDiff> Differences { get; set; } = new();
}
