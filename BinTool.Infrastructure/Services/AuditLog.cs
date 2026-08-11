using System.Text.Json;
using System.Text.Json.Serialization;
using BinTool.Domain.Entities;
using BinTool.Application.Abstractions;
using BinTool.Infrastructure.Data;

namespace BinTool.Infrastructure.Services;

public class AuditLog : IAuditLog
{
    // Nulls are kept rather than dropped: in a before/after comparison "this field was empty" and
    // "this field was not recorded" have to stay tellable apart.
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.Never,
        WriteIndented = false
    };

    private readonly AppDbContext _db;
    private readonly ICurrentUser _currentUser;

    public AuditLog(AppDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public void Record(
        AuditAction action, string entityType, int entityId,
        object? oldValues, object? newValues)
    {
        _db.AuditEntries.Add(new AuditEntry
        {
            EntityType = entityType,
            EntityId = entityId,
            Action = action,

            OldValues = Serialize(oldValues),
            NewValues = Serialize(newValues),

            // The id, not the name: a name can be changed, and the entry has to keep
            // pointing at the same account. Null when nobody was signed in.
            PerformedByUserId = _currentUser.UserId,
            PerformedAt = DateTime.UtcNow
        });
    }

    private static string? Serialize(object? values) =>
        values is null ? null : JsonSerializer.Serialize(values, Json);
}
