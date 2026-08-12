namespace BinTool.Application.Models.Import;

public class ImportHistoryItem
{
    public int ImportHistoryId { get; set; }

    public string FileName { get; set; } = string.Empty;

    public DateTime ImportedAt { get; set; }

    public int ImportedRows { get; set; }

    public int UpdatedRows { get; set; }

    public int RejectedRows { get; set; }

    public string Status { get; set; } = string.Empty;

    public string UserName { get; set; } = string.Empty;
}
