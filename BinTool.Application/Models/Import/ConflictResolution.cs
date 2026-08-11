namespace BinTool.Application.Models.Import;

/// <summary>A user's decision for a single staged conflict.</summary>
public class ConflictResolution
{
    /// <summary>
    /// Id of the staged conflict this decision applies to, as returned by the import or
    /// by <c>GET /api/BinCsvImport/conflicts</c>.
    /// </summary>
    public int PendingBinConflictId { get; set; }

    /// <summary>
    /// True to overwrite the existing BIN range with the imported values;
    /// false to keep the existing record and discard the incoming row.
    /// </summary>
    public bool Update { get; set; }
}
