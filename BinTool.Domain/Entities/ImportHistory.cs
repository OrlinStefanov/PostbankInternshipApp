namespace BinTool.Domain.Entities;

public class ImportHistory
{
    public int ImportHistoryId { get; set; }

    public string FileName { get; set; } = string.Empty;

    public int ImportedRows { get; set; }

    // Grows after the run: a staged conflict that is later applied counts here, not against
    // the import that first saw it.
    public int UpdatedRows { get; set; }

    public int RejectedRows { get; set; }

    public string? ImportedByUserId { get; set; }

    public DateTime ImportedAt { get; set; } = DateTime.UtcNow;

    // Success, Partial or Failed.
    public string Status { get; set; } = "Success";

    #region Navigation Properties

    public ApplicationUser? ImportedByUser { get; set; }

    // Named apart from the RejectedRows count above, which is a different thing with the
    // same obvious name.
    public ICollection<RejectedImportRow> RejectedRows_Navigation { get; set; } = new List<RejectedImportRow>();

    #endregion
}
