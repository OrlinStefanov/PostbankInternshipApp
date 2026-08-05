using BinTool.Core.Entities;

namespace BinTool.Core.Services;

/// <summary>
/// Records what changed, to whom it belonged and who changed it.
/// <para>
/// Entries are staged, not saved: they are written by the caller's own
/// <c>SaveChanges</c>, in the same transaction as the change they describe. An audit row
/// that could be committed separately from its change would eventually disagree with it,
/// which is worse than having no audit row at all.
/// </para>
/// </summary>
public interface IAuditLog
{
    /// <summary>
    /// Stages one entry.
    /// </summary>
    /// <param name="action">What was done.</param>
    /// <param name="entityType">One of <see cref="AuditEntityTypes"/>.</param>
    /// <param name="entityId">Key of the row that changed. It must already exist - an
    /// insert has to be saved before it can be audited, so its id is known.</param>
    /// <param name="oldValues">The state before, or null for an insert.</param>
    /// <param name="newValues">The state after, or null for a hard delete.</param>
    void Record(
        AuditAction action, string entityType, int entityId,
        object? oldValues, object? newValues);
}
