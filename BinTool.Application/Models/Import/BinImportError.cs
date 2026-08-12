namespace BinTool.Application.Models.Import;

public class BinImportError
{
    public int RowNumber { get; set; }

    public string Reason { get; set; } = string.Empty;

    public string RawData { get; set; } = string.Empty;
}
