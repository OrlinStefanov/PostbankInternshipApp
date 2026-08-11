using BinTool.Domain.Entities;

namespace BinTool.Application.Models.Audit;

/// <summary>
/// One audit entry as it appears in a browse listing. <see cref="OldValues"/> and
/// <see cref="NewValues"/> are the raw JSON snapshots the writer stored - the client
/// decides how to render them.
/// </summary>
public class AuditLogItem
{
    public int AuditEntryId { get; set; }

    /// <summary>UTC timestamp of the change.</summary>
    public DateTime PerformedAt { get; set; }

    public AuditAction Action { get; set; }

    /// <summary>One of the <see cref="AuditEntityTypes"/> constants.</summary>
    public string EntityType { get; set; } = string.Empty;

    public int EntityId { get; set; }

    /// <summary>
    /// User name of whoever made the change. The literal <c>system</c> means no user was
    /// signed in - a row written before the change reached the audit path, or by a
    /// background task.
    /// </summary>
    public string UserName { get; set; } = string.Empty;

    /// <summary>
    /// JSON snapshot of the row before the change. Null on a Create or an Import.
    /// </summary>
    public string? OldValues { get; set; }

    /// <summary>JSON snapshot of the row after the change. Null on a hard delete.</summary>
    public string? NewValues { get; set; }
}
