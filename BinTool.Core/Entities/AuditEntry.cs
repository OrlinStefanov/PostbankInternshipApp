namespace BinTool.Core.Entities;

/// <summary>
/// Audit trail record for tracking all configuration changes
/// </summary>
public class AuditEntry
{
    public int AuditEntryId { get; set; }

    /// <summary>
    /// Type of entity that was modified (e.g., "BinRange", "CommissionRule")
    /// </summary>
    public string EntityType { get; set; } = string.Empty;

    /// <summary>
    /// Primary key/ID of the entity that was modified
    /// </summary>
    public int EntityId { get; set; }

    /// <summary>
    /// Type of change: Created, Updated, Deleted, Deactivated, or Imported
    /// </summary>
    public AuditAction Action { get; set; }

    /// <summary>
    /// JSON snapshot of the previous values (for updates/deletes)
    /// </summary>
    public string? OldValues { get; set; }

    /// <summary>
    /// JSON snapshot of the new values (for creates/updates)
    /// </summary>
    public string? NewValues { get; set; }

    /// <summary>
    /// User ID who performed the action
    /// </summary>
    public string? PerformedByUserId { get; set; }

    /// <summary>
    /// Timestamp when the action was performed
    /// </summary>
    public DateTime PerformedAt { get; set; } = DateTime.UtcNow;
}
