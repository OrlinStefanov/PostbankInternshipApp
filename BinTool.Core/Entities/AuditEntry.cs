namespace BinTool.Core.Entities;

/// <summary>
/// Audit trail record for tracking all configuration changes
/// </summary>
public class AuditEntry
{
    public int Id { get; set; }

    /// <summary>
    /// Type of entity that was modified (e.g., "BinRange", "CommissionRule")
    /// </summary>
    public string EntityType { get; set; } = string.Empty;

    /// <summary>
    /// Primary key/ID of the entity that was modified
    /// </summary>
    public int EntityId { get; set; }

    /// <summary>
    /// Type of change: Created, Updated, Deleted, or Deactivated
    /// </summary>
    public AuditAction Action { get; set; }

    /// <summary>
    /// User ID who made the change
    /// </summary>
    public string? UserId { get; set; }

    /// <summary>
    /// Username of who made the change (for easier reading)
    /// </summary>
    public string? UserName { get; set; }

    /// <summary>
    /// JSON snapshot of the previous values (for updates/deletes)
    /// </summary>
    public string? OldValues { get; set; }

    /// <summary>
    /// JSON snapshot of the new values (for creates/updates)
    /// </summary>
    public string? NewValues { get; set; }

    /// <summary>
    /// Names of properties that were changed
    /// </summary>
    public string? ChangedProperties { get; set; }

    /// <summary>
    /// Optional description of why the change was made
    /// </summary>
    public string? ChangeReason { get; set; }

    /// <summary>
    /// Timestamp when the change was made
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// IP address or client identifier (if available)
    /// </summary>
    public string? ClientInfo { get; set; }
}

/// <summary>
/// Types of changes that can be audited
/// </summary>
public enum AuditAction
{
    Created = 1,
    Updated = 2,
    Deleted = 3,
    Deactivated = 4,
    Imported = 5,
}
