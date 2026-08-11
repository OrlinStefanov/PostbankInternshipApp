using BinTool.Infrastructure.Data;

namespace BinTool.Infrastructure.Repositories;

public class BinImportRepository : IBinImportRepository
{
    /// <summary>
    /// Lookups are batched so a large file does not build a single enormous IN (...) clause,
    /// which is slow to plan and can exceed the provider's parameter limit (SQLite caps host
    /// parameters per statement).
    /// </summary>
    private const int BatchSize = 500;

    private readonly AppDbContext _db;

    public BinImportRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task<ReferenceTables> LoadReferenceTablesAsync(
        CancellationToken cancellationToken = default)
    {
        var cardSchemes = await _db.CardSchemes.AsNoTracking().Where(c => !c.IsDeleted)
            .Select(c => new { Id = c.CardSchemeId, c.Name }).ToListAsync(cancellationToken);

        var productTypes = await _db.ProductTypes.AsNoTracking().Where(p => !p.IsDeleted)
            .Select(p => new { Id = p.ProductTypeId, p.Name }).ToListAsync(cancellationToken);

        var fundingTypes = await _db.FundingTypes.AsNoTracking().Where(f => !f.IsDeleted)
            .Select(f => new { Id = f.FundingTypeId, f.Name }).ToListAsync(cancellationToken);

        var countries = await _db.Countries.AsNoTracking().Where(c => !c.IsDeleted)
            .Select(c => new { Id = c.CountryId, Name = c.IsoCode }).ToListAsync(cancellationToken);

        // Name to id is case-insensitive because a file may spell "visa"; id to name gives
        // back the stored spelling, so a diff never shows a case difference as a change.
        return new ReferenceTables(
            cardSchemes.ToDictionary(x => x.Name, x => x.Id, StringComparer.OrdinalIgnoreCase),
            productTypes.ToDictionary(x => x.Name, x => x.Id, StringComparer.OrdinalIgnoreCase),
            fundingTypes.ToDictionary(x => x.Name, x => x.Id, StringComparer.OrdinalIgnoreCase),
            countries.ToDictionary(x => x.Name, x => x.Id, StringComparer.OrdinalIgnoreCase),
            cardSchemes.ToDictionary(x => x.Id, x => x.Name),
            productTypes.ToDictionary(x => x.Id, x => x.Name),
            fundingTypes.ToDictionary(x => x.Id, x => x.Name),
            countries.ToDictionary(x => x.Id, x => x.Name));
    }

    public void AddHistory(ImportHistory history) => _db.ImportHistories.Add(history);

    public async Task<IReadOnlyList<ImportHistory>> GetHistoriesAsync(
        IReadOnlyCollection<int> historyIds, CancellationToken cancellationToken = default)
    {
        var ids = historyIds.ToList();
        if (ids.Count == 0) return Array.Empty<ImportHistory>();

        return await _db.ImportHistories
            .Where(h => ids.Contains(h.ImportHistoryId))
            .ToListAsync(cancellationToken);
    }

    public Task<IReadOnlyList<BinRange>> FindByPrefixesAsync(
        IReadOnlyCollection<string> prefixes, CancellationToken cancellationToken = default) =>
        InBatchesAsync(prefixes, (batch, ct) => _db.BinRanges
            .AsNoTracking()
            .Where(b => batch.Contains(b.Prefix))
            .ToListAsync(ct));

    public Task<IReadOnlyList<BinRange>> GetRangesForUpdateAsync(
        IReadOnlyCollection<int> binRangeIds, CancellationToken cancellationToken = default) =>
        InBatchesAsync(binRangeIds, (batch, ct) => _db.BinRanges
            .Where(b => batch.Contains(b.BinRangeId))
            .ToListAsync(ct));

    public Task<IReadOnlyList<BinRange>> GetRangesAsync(
        IReadOnlyCollection<int> binRangeIds, CancellationToken cancellationToken = default) =>
        InBatchesAsync(binRangeIds, (batch, ct) => _db.BinRanges
            .AsNoTracking()
            .Where(b => batch.Contains(b.BinRangeId))
            .ToListAsync(ct));

    public void AddRange(BinRange range) => _db.BinRanges.Add(range);

    public void AddConflict(PendingBinConflict conflict) =>
        _db.Set<PendingBinConflict>().Add(conflict);

    public async Task<IReadOnlyList<PendingBinConflict>> GetPendingForUpdateAsync(
        IReadOnlyCollection<int> conflictIds, CancellationToken cancellationToken = default)
    {
        var ids = conflictIds.ToList();
        if (ids.Count == 0) return Array.Empty<PendingBinConflict>();

        return await _db.Set<PendingBinConflict>()
            .Where(c => ids.Contains(c.PendingBinConflictId) && c.Status == ConflictStatus.Pending)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<PendingBinConflict>> ListPendingAsync(
        CancellationToken cancellationToken = default) =>
        await _db.Set<PendingBinConflict>()
            .AsNoTracking()
            .Where(c => c.Status == ConflictStatus.Pending)
            .OrderBy(c => c.PendingBinConflictId)
            .ToListAsync(cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _db.SaveChangesAsync(cancellationToken);

    public async Task<ITransaction> BeginTransactionAsync(CancellationToken cancellationToken = default) =>
        new EfTransaction(await _db.Database.BeginTransactionAsync(cancellationToken));

    private static async Task<IReadOnlyList<T>> InBatchesAsync<TKey, T>(
        IReadOnlyCollection<TKey> keys, Func<TKey[], CancellationToken, Task<List<T>>> query,
        CancellationToken cancellationToken = default)
    {
        if (keys.Count == 0) return Array.Empty<T>();

        var results = new List<T>(keys.Count);
        foreach (var batch in keys.Chunk(BatchSize))
        {
            results.AddRange(await query(batch, cancellationToken));
        }

        return results;
    }
}
