using BinTool.Domain.Entities;

namespace BinTool.Application.Abstractions;

public interface IAuditLog
{
    /// <summary>Stages one entry.</summary>
    void Record(
        AuditAction action, string entityType, int entityId,
        object? oldValues, object? newValues);
}
