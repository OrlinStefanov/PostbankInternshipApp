namespace BinTool.Core.Models.Import;

/// <summary>
/// A user's decision for a single staged conflict.
/// </summary>
public class ConflictResolution
{
    public int PendingBinConflictId { get; set; }

    /// <summary>
    /// True to overwrite the existing BIN range with the imported values;
    /// false to keep the existing record and discard the incoming row.
    /// </summary>
    public bool Update { get; set; }
}
