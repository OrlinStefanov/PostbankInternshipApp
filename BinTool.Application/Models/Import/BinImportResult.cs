namespace BinTool.Application.Models.Import;

public class BinImportResult
{
    /// <summary>Name of the uploaded file.</summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>Id of the <c>ImportHistory</c> record written for this run.</summary>
    public int ImportHistoryId { get; set; }

    /// <summary>
    /// Total data rows read from the file (inserted + unchanged + conflicts + rejected).
    /// </summary>
    public int TotalRows { get; set; }

    /// <summary>New prefixes added to the database.</summary>
    public int InsertedCount { get; set; }

    /// <summary>Existing prefixes whose values matched exactly and were skipped.</summary>
    public int UnchangedCount { get; set; }

    /// <summary>Existing prefixes with differing values, staged for a decision.</summary>
    public int ConflictCount { get; set; }

    /// <summary>Rows rejected due to bad structure or unknown lookup values.</summary>
    public int RejectedCount { get; set; }

    /// <summary>
    /// Staged conflicts, each with a per-field diff, awaiting the user's decision.
    /// </summary>
    public List<BinConflict> Conflicts { get; set; } = new();

    /// <summary>Rows that were rejected, with the row number and reason.</summary>
    public List<BinImportError> Errors { get; set; } = new();
}
