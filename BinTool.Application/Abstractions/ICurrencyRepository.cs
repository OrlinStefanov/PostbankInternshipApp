namespace BinTool.Application.Abstractions;

public interface ICurrencyRepository
{
    Task<IReadOnlyList<Currency>> ListAsync(
        bool includeDeleted, CancellationToken cancellationToken = default);

    Task<Currency?> GetAsync(int id, CancellationToken cancellationToken = default);

    Task<Currency?> GetForUpdateAsync(int id, CancellationToken cancellationToken = default);

    Task<Currency?> FindByCodeAsync(string code, CancellationToken cancellationToken = default);

    /// <summary>A live currency by id, for pricing.</summary>
    Task<Currency?> GetLiveAsync(int id, CancellationToken cancellationToken = default);

    Task<int?> FindLiveIdByCodeAsync(string? code, CancellationToken cancellationToken = default);

    /// <summary>The live euro row, the base every rate is expressed against.</summary>
    Task<Currency?> GetBaseCurrencyAsync(CancellationToken cancellationToken = default);

    /// <summary>How many live commission rules are still priced in this currency.</summary>
    Task<int> CountRulesUsingAsync(int currencyId, CancellationToken cancellationToken = default);

    void Add(Currency currency);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);

    Task<ITransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);
}
