using BinTool.Domain.Entities;

namespace BinTool.Application.Models.Audit;

public class AuditLogItem
{
    public int AuditEntryId { get; set; }

    public DateTime PerformedAt { get; set; }

    public AuditAction Action { get; set; }

    public string EntityType { get; set; } = string.Empty;

    public int EntityId { get; set; }

    public string UserName { get; set; } = string.Empty;

    public string? OldValues { get; set; }

    public string? NewValues { get; set; }
}
