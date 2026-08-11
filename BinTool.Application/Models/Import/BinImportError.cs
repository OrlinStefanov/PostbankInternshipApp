namespace BinTool.Application.Models.Import;

/// <summary>
/// A single row that failed validation during import.
/// Maps directly onto the RejectedImportRow entity when persisted (Phase 5).
/// </summary>
public class BinImportError
{
    /// <summary>
    /// 1-based row number in the original CSV (the data row, not counting the header).
    /// </summary>
    public int RowNumber { get; set; }

    /// <summary>Why the row was rejected (e.g. "Prefix must be 6-8 digits").</summary>
    public string Reason { get; set; } = string.Empty;

    /// <summary>The raw row content, kept for inspection/debugging.</summary>
    public string RawData { get; set; } = string.Empty;
}
