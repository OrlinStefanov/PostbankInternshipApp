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
