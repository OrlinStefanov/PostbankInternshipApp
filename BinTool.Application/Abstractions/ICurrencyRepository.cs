namespace BinTool.Application.Abstractions;

/// <summary>
/// Storage for currencies. Returns materialized results, never a query the caller could go
/// on building - a caller that can extend the query is a caller that decides what SQL runs.
/// </summary>
public interface ICurrencyRepository
{
    Task<IReadOnlyList<Currency>> ListAsync(
        bool includeDeleted, CancellationToken cancellationToken = default);

    Task<Currency?> GetAsync(int id, CancellationToken cancellationToken = default);

    Task<Currency?> GetForUpdateAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds a currency by code across live and soft-deleted rows, because the code is
    /// unique over both - a deleted code revives its row rather than allowing a duplicate.
    /// </summary>
    Task<Currency?> FindByCodeAsync(string code, CancellationToken cancellationToken = default);

    /// <summary>How many live commission rules are still priced in this currency.</summary>
    Task<int> CountRulesUsingAsync(int currencyId, CancellationToken cancellationToken = default);

    void Add(Currency currency);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);

    Task<ITransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);
}
