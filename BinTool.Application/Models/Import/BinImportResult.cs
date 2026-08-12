namespace BinTool.Application.Models.Import;

public class BinImportResult
{
    public string FileName { get; set; } = string.Empty;

    public int ImportHistoryId { get; set; }

    public int TotalRows { get; set; }

    public int InsertedCount { get; set; }

    public int UnchangedCount { get; set; }

    public int ConflictCount { get; set; }

    public int RejectedCount { get; set; }

    public List<BinConflict> Conflicts { get; set; } = new();

    public List<BinImportError> Errors { get; set; } = new();
}
