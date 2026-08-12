using BinTool.Application.Models.ReferenceData;

namespace BinTool.Application.Abstractions;

public interface ILookupRepository
{
    Task<IReadOnlyList<ILookupEntity>> ListAsync(
        LookupKind kind, bool includeDeleted, CancellationToken cancellationToken = default);

    Task<ILookupEntity?> GetAsync(
        LookupKind kind, int id, CancellationToken cancellationToken = default);

    Task<ILookupEntity?> GetForUpdateAsync(
        LookupKind kind, int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds a row by name, case-insensitively, across live and soft-deleted rows: the unique index
    /// spans both, so a deleted name revives its row rather than duplicating.
    /// </summary>
    Task<ILookupEntity?> FindByNameAsync(
        LookupKind kind, string name, CancellationToken cancellationToken = default);

    /// <summary>Creates and stages a new row.</summary>
    ILookupEntity Add(LookupKind kind, string name, string? description);

    /// <summary>How many live rows would be orphaned if this one were deleted.</summary>
    Task<int> CountLiveReferencesAsync(
        LookupKind kind, int id, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);

    Task<ITransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);
}
