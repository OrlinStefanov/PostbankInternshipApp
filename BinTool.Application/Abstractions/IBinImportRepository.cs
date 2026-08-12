using BinTool.Application.Models.Import;

namespace BinTool.Application.Abstractions;

public interface IBinImportRepository
{
    Task<ReferenceTables> LoadReferenceTablesAsync(CancellationToken cancellationToken = default);

    void AddHistory(ImportHistory history);

    Task<IReadOnlyList<ImportHistory>> GetHistoriesAsync(
        IReadOnlyCollection<int> historyIds, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<BinRange>> FindByPrefixesAsync(
        IReadOnlyCollection<string> prefixes, CancellationToken cancellationToken = default);

    /// <summary>Ranges by id, tracked, so the caller can change them.</summary>
    Task<IReadOnlyList<BinRange>> GetRangesForUpdateAsync(
        IReadOnlyCollection<int> binRangeIds, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<BinRange>> GetRangesAsync(
        IReadOnlyCollection<int> binRangeIds, CancellationToken cancellationToken = default);

    void AddRange(BinRange range);

    void AddConflict(PendingBinConflict conflict);

    /// <summary>
    /// Still-pending conflicts among the given ids, tracked so they can be resolved.
    /// </summary>
    Task<IReadOnlyList<PendingBinConflict>> GetPendingForUpdateAsync(
        IReadOnlyCollection<int> conflictIds, CancellationToken cancellationToken = default);

    /// <summary>Every pending conflict, oldest first, for the worklist.</summary>
    Task<IReadOnlyList<PendingBinConflict>> ListPendingAsync(
        CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);

    Task<ITransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);
}
