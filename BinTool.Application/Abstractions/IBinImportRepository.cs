namespace BinTool.Application.Abstractions;

public interface IBinImportRepository
{
    /// <summary>
    /// The live reference tables, keyed both ways: name to id, and id back to name.
    /// </summary>
    Task<ReferenceTables> LoadReferenceTablesAsync(CancellationToken cancellationToken = default);

    void AddHistory(ImportHistory history);

    Task<IReadOnlyList<ImportHistory>> GetHistoriesAsync(
        IReadOnlyCollection<int> historyIds, CancellationToken cancellationToken = default);

    /// <summary>
    /// Existing ranges for these prefixes, soft-deleted rows included: the unique index on Prefix
    /// spans them, so inserting alongside one would violate the constraint.
    /// </summary>
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

public sealed record ReferenceTables(
    IReadOnlyDictionary<string, int> CardSchemeIds,
    IReadOnlyDictionary<string, int> ProductTypeIds,
    IReadOnlyDictionary<string, int> FundingTypeIds,
    IReadOnlyDictionary<string, int> CountryIds,
    IReadOnlyDictionary<int, string> CardSchemeNames,
    IReadOnlyDictionary<int, string> ProductTypeNames,
    IReadOnlyDictionary<int, string> FundingTypeNames,
    IReadOnlyDictionary<int, string> CountryCodes);
