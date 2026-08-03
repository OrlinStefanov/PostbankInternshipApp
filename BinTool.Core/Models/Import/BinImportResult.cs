namespace BinTool.Core.Models.Import;

/// <summary>
/// Outcome of a single import run. Splits the file into the rows that passed
/// validation and the rows that were rejected, each with a reason.
/// Nothing is saved to the database at this stage.
/// </summary>
public class BinImportResult
{
    /// <summary>
    /// Name of the uploaded file.
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// Total data rows read from the file (valid + rejected).
    /// </summary>
    public int TotalRows { get; set; }

    /// <summary>
    /// Number of rows that passed validation. Equals <see cref="ValidRows"/>.Count.
    /// </summary>
    public int ValidCount { get; set; }

    /// <summary>
    /// Number of rows that were rejected. Equals <see cref="Errors"/>.Count.
    /// </summary>
    public int RejectedCount { get; set; }

    /// <summary>
    /// Rows that passed validation, parsed into typed values.
    /// </summary>
    public List<BinImportRow> ValidRows { get; set; } = new();

    /// <summary>
    /// Rows that failed validation, with the row number and reason.
    /// </summary>
    public List<BinImportError> Errors { get; set; } = new();
}
