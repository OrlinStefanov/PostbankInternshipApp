namespace BinTool.Application.Models.Import;

public class ImportHistoryItem
{
    public int ImportHistoryId { get; set; }

    /// <summary>Name of the uploaded file.</summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>UTC timestamp of when the import ran.</summary>
    public DateTime ImportedAt { get; set; }

    /// <summary>
    /// New BIN ranges inserted by this import (including revived soft-deleted prefixes).
    /// </summary>
    public int ImportedRows { get; set; }

    /// <summary>
    /// Existing BIN ranges this import ultimately updated, counted as its staged conflicts are
    /// applied.
    /// </summary>
    public int UpdatedRows { get; set; }

    /// <summary>Rows rejected by validation.</summary>
    public int RejectedRows { get; set; }

    /// <summary>Recorded status - Success, Partial or Failed.</summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>User name of whoever ran the import.</summary>
    public string UserName { get; set; } = string.Empty;
}
