namespace BinTool.Application.Abstractions;

/// <summary>
/// Storage for a BIN import run and the conflicts it leaves behind.
/// <para>
/// Prefixes and ids are looked up in batches rather than one row at a time: a large file
/// would otherwise be one query per row, and a single enormous IN (...) clause is slow to
/// plan and can exceed SQLite's per-statement parameter limit. The batch size is the
/// repository's business, so callers hand over the whole set and let it decide.
/// </para>
/// </summary>
public interface IBinImportRepository
{
    /// <summary>The live reference tables, keyed both ways: name to id, and id back to name.</summary>
    Task<ReferenceTables> LoadReferenceTablesAsync(CancellationToken cancellationToken = default);

    void AddHistory(ImportHistory history);

    Task<IReadOnlyList<ImportHistory>> GetHistoriesAsync(
        IReadOnlyCollection<int> historyIds, CancellationToken cancellationToken = default);

    /// <summary>
    /// Existing ranges for these prefixes, soft-deleted rows included: the unique index on
    /// Prefix spans them, so inserting alongside one would violate the constraint.
    /// Untracked - this is the compare path, and the rare revival is re-read tracked.
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

    /// <summary>Still-pending conflicts among the given ids, tracked so they can be resolved.</summary>
    Task<IReadOnlyList<PendingBinConflict>> GetPendingForUpdateAsync(
        IReadOnlyCollection<int> conflictIds, CancellationToken cancellationToken = default);

    /// <summary>Every pending conflict, oldest first, for the worklist.</summary>
    Task<IReadOnlyList<PendingBinConflict>> ListPendingAsync(
        CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);

    Task<ITransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// The reference tables as the import needs them: name to id to resolve an incoming row,
/// and id back to name to describe a stored one in a diff.
/// </summary>
public sealed record ReferenceTables(
    IReadOnlyDictionary<string, int> CardSchemeIds,
    IReadOnlyDictionary<string, int> ProductTypeIds,
    IReadOnlyDictionary<string, int> FundingTypeIds,
    IReadOnlyDictionary<string, int> CountryIds,
    IReadOnlyDictionary<int, string> CardSchemeNames,
    IReadOnlyDictionary<int, string> ProductTypeNames,
    IReadOnlyDictionary<int, string> FundingTypeNames,
    IReadOnlyDictionary<int, string> CountryCodes);
