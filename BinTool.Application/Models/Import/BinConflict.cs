namespace BinTool.Application.Models.Import;

public class BinConflict
{
    public int PendingBinConflictId { get; set; }

    public int RowNumber { get; set; }

    public string Prefix { get; set; } = string.Empty;

    public string ConflictType { get; set; } = "ValueConflict";

    public string? Message { get; set; }

    public List<BinFieldDiff> Differences { get; set; } = new();

    public string? DetectedScheme { get; set; }

    public string? SchemeAdvisory { get; set; }
}
