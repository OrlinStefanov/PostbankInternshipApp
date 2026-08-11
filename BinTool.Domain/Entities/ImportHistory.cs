namespace BinTool.Domain.Entities;

/// <summary>
/// Track bulk import operations for audit and recovery
/// </summary>
public class ImportHistory
{
    public int ImportHistoryId { get; set; }

    /// <summary>
    /// Name of the imported file
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// Number of rows in the imported file
    /// </summary>
    public int ImportedRows { get; set; }

    /// <summary>
    /// Number of rows that were updated (existing records)
    /// </summary>
    public int UpdatedRows { get; set; }

    /// <summary>
    /// Number of rows that were rejected due to validation errors
    /// </summary>
    public int RejectedRows { get; set; }

    /// <summary>
    /// User ID who performed the import
    /// </summary>
    public string? ImportedByUserId { get; set; }

    /// <summary>
    /// When the import was performed
    /// </summary>
    public DateTime ImportedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Status of the import (Success, Partial, Failed)
    /// </summary>
    public string Status { get; set; } = "Success";

    #region Navigation Properties

    /// <summary>
    /// User who performed the import
    /// </summary>
    public ApplicationUser? ImportedByUser { get; set; }

    /// <summary>
    /// Rejected rows from this import
    /// </summary>
    public ICollection<RejectedImportRow> RejectedRows_Navigation { get; set; } = new List<RejectedImportRow>();

    #endregion
}
