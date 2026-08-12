using BinTool.Domain.Entities;

namespace BinTool.Application.Models.Audit;

public class AuditLogItem
{
    public int AuditEntryId { get; set; }

    /// <summary>UTC timestamp of the change.</summary>
    public DateTime PerformedAt { get; set; }

    public AuditAction Action { get; set; }

    /// <summary>One of the <see cref="AuditEntityTypes"/> constants.</summary>
    public string EntityType { get; set; } = string.Empty;

    public int EntityId { get; set; }

    /// <summary>User name of whoever made the change.</summary>
    public string UserName { get; set; } = string.Empty;

    /// <summary>JSON snapshot of the row before the change.</summary>
    public string? OldValues { get; set; }

    /// <summary>JSON snapshot of the row after the change.</summary>
    public string? NewValues { get; set; }
}
