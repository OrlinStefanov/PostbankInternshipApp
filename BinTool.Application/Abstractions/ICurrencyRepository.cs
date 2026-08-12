namespace BinTool.Application.Abstractions;

public interface ICurrencyRepository
{
    Task<IReadOnlyList<Currency>> ListAsync(
        bool includeDeleted, CancellationToken cancellationToken = default);

    Task<Currency?> GetAsync(int id, CancellationToken cancellationToken = default);

    Task<Currency?> GetForUpdateAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds a currency by code across live and soft-deleted rows, because the code is unique over
    /// both - a deleted code revives its row rather than allowing a duplicate.
    /// </summary>
    Task<Currency?> FindByCodeAsync(string code, CancellationToken cancellationToken = default);

    /// <summary>A live currency by id, for pricing.</summary>
    Task<Currency?> GetLiveAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// The id of the live currency with this ISO-4217 code, or null when the code is blank or names
    /// nothing.
    /// </summary>
    Task<int?> FindLiveIdByCodeAsync(string? code, CancellationToken cancellationToken = default);

    /// <summary>The live euro row, the base every rate is expressed against.</summary>
    Task<Currency?> GetBaseCurrencyAsync(CancellationToken cancellationToken = default);

    /// <summary>How many live commission rules are still priced in this currency.</summary>
    Task<int> CountRulesUsingAsync(int currencyId, CancellationToken cancellationToken = default);

    void Add(Currency currency);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);

    Task<ITransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);
}
