namespace BinTool.Application.Models.Import;

/// <summary>
/// Outcome of an import run. New rows are inserted and rejected rows are
/// persisted immediately; unchanged rows are skipped. Rows whose prefix already
/// exists with different values are staged as <see cref="Conflicts"/> for the
/// user to resolve afterwards.
/// </summary>
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

    /// <summary>
    /// New prefixes added to the database. Includes prefixes whose only existing record
    /// was soft-deleted and has been revived in place.
    /// </summary>
    public int InsertedCount { get; set; }

    /// <summary>Existing prefixes whose values matched exactly and were skipped.</summary>
    public int UnchangedCount { get; set; }

    /// <summary>
    /// Existing prefixes with differing values, staged for a decision. Always the number
    /// of entries in <c>conflicts</c>.
    /// </summary>
    public int ConflictCount { get; set; }

    /// <summary>
    /// Rows rejected due to bad structure or unknown lookup values. Always the number of
    /// entries in <c>errors</c>.
    /// </summary>
    public int RejectedCount { get; set; }

    /// <summary>
    /// Staged conflicts, each with a per-field diff, awaiting the user's decision.
    /// </summary>
    public List<BinConflict> Conflicts { get; set; } = new();

    /// <summary>Rows that were rejected, with the row number and reason.</summary>
    public List<BinImportError> Errors { get; set; } = new();
}
