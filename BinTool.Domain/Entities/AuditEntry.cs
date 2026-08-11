namespace BinTool.Domain.Entities;

public class AuditEntry
{
    public int AuditEntryId { get; set; }

    public string EntityType { get; set; } = string.Empty;

    public int EntityId { get; set; }

    public AuditAction Action { get; set; }

    public string? OldValues { get; set; }

    public string? NewValues { get; set; }

    public string? PerformedByUserId { get; set; }

    public DateTime PerformedAt { get; set; } = DateTime.UtcNow;

    #region Navigation Properties

    public ApplicationUser? PerformedByUser { get; set; }

    #endregion
}
