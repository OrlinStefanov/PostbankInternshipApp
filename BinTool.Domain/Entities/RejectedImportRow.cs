namespace BinTool.Domain.Entities;

public class RejectedImportRow
{
    public int RejectedRowId { get; set; }

    public int ImportHistoryId { get; set; }

    public int RowNumber { get; set; }

    public string Reason { get; set; } = string.Empty;

    public string RawData { get; set; } = string.Empty;

    #region Navigation Properties

    public ImportHistory? ImportHistory { get; set; }

    #endregion
}
