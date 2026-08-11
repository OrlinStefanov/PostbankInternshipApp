using BinTool.Application.Models.ReferenceData;

namespace BinTool.Application.Abstractions;

/// <summary>
/// Storage for the four Name+Description reference tables. The <see cref="LookupKind"/>
/// chooses the table; everything above this interface treats all four the same, because
/// <see cref="ILookupEntity"/> is the only shape they need.
/// </summary>
public interface ILookupRepository
{
    Task<IReadOnlyList<ILookupEntity>> ListAsync(
        LookupKind kind, bool includeDeleted, CancellationToken cancellationToken = default);

    Task<ILookupEntity?> GetAsync(
        LookupKind kind, int id, CancellationToken cancellationToken = default);

    Task<ILookupEntity?> GetForUpdateAsync(
        LookupKind kind, int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds a row by name, case-insensitively, across live and soft-deleted rows: the
    /// unique index spans both, so a deleted name revives its row rather than duplicating.
    /// Tracked, because the caller may be about to revive it.
    /// </summary>
    Task<ILookupEntity?> FindByNameAsync(
        LookupKind kind, string name, CancellationToken cancellationToken = default);

    /// <summary>Creates and stages a new row. Unsaved, so it has no id yet.</summary>
    ILookupEntity Add(LookupKind kind, string name, string? description);

    /// <summary>How many live rows would be orphaned if this one were deleted.</summary>
    Task<int> CountLiveReferencesAsync(
        LookupKind kind, int id, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);

    Task<ITransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);
}
