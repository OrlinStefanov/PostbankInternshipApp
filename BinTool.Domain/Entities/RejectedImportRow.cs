namespace BinTool.Domain.Entities;

/// <summary>
/// Tracks rows that were rejected during bulk import operations
/// Used for audit trail and user feedback
/// </summary>
public class RejectedImportRow
{
    public int RejectedRowId { get; set; }

    /// <summary>
    /// Import history this rejection belongs to
    /// </summary>
    public int ImportHistoryId { get; set; }

    /// <summary>
    /// Row number in the original file (1-based)
    /// </summary>
    public int RowNumber { get; set; }

    /// <summary>
    /// Reason for rejection (validation error message)
    /// </summary>
    public string Reason { get; set; } = string.Empty;

    /// <summary>
    /// Raw data from the rejected row (for inspection/debugging)
    /// </summary>
    public string RawData { get; set; } = string.Empty;

    #region Navigation Properties

    public ImportHistory? ImportHistory { get; set; }

    #endregion
}
