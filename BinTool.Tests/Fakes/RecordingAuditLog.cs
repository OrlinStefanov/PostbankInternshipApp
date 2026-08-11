namespace BinTool.Tests.Fakes;

public sealed class RecordingAuditLog : IAuditLog
{
    public List<AuditRecord> Entries { get; } = new();

    public void Record(
        AuditAction action, string entityType, int entityId,
        object? oldValues, object? newValues) =>
        Entries.Add(new AuditRecord(action, entityType, entityId, oldValues, newValues));

    public sealed record AuditRecord(
        AuditAction Action, string EntityType, int EntityId, object? Old, object? New);
}

public sealed class StubCurrentUser : ICurrentUser
{
    public StubCurrentUser(string name = "admin", string? userId = "admin-user-id")
    {
        Name = name;
        UserId = userId;
    }

    public string? UserId { get; }

    public string Name { get; }
}
