using System.Text.Json;
using System.Text.Json.Serialization;
using BinTool.Application.Abstractions;
using BinTool.Domain.Entities;

namespace BinTool.Application.Services;

public class AuditLog : IAuditLog
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.Never,
        WriteIndented = false
    };

    private readonly IAuditRepository _audit;
    private readonly ICurrentUser _currentUser;

    public AuditLog(IAuditRepository audit, ICurrentUser currentUser)
    {
        _audit = audit;
        _currentUser = currentUser;
    }

    public void Record(
        AuditAction action, string entityType, int entityId,
        object? oldValues, object? newValues)
    {
        _audit.Add(new AuditEntry
        {
            EntityType = entityType,
            EntityId = entityId,
            Action = action,

            OldValues = Serialize(oldValues),
            NewValues = Serialize(newValues),

            PerformedByUserId = _currentUser.UserId,
            PerformedAt = DateTime.UtcNow
        });
    }

    private static string? Serialize(object? values) =>
        values is null ? null : JsonSerializer.Serialize(values, Json);
}
