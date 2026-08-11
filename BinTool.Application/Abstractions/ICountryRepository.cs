namespace BinTool.Application.Abstractions;

/// <summary>
/// Storage for countries. Returns materialized results, never a query the caller could go
/// on building.
/// </summary>
public interface ICountryRepository
{
    /// <summary>Countries with their region loaded, ordered by ISO code.</summary>
    Task<IReadOnlyList<Country>> ListAsync(
        bool includeDeleted, CancellationToken cancellationToken = default);

    Task<Country?> GetWithRegionAsync(int id, CancellationToken cancellationToken = default);

    Task<Country?> GetForUpdateAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds a country by ISO code across live and soft-deleted rows: the unique index spans
    /// both, so a deleted code revives its row rather than allowing a duplicate.
    /// </summary>
    Task<Country?> FindByIsoCodeAsync(string isoCode, CancellationToken cancellationToken = default);

    /// <summary>A live region by id, or null. Deleted regions cannot be assigned to.</summary>
    Task<RegionIdentity?> FindLiveRegionAsync(
        int regionId, CancellationToken cancellationToken = default);

    /// <summary>The stored name of any region, deleted included, for a before-snapshot.</summary>
    Task<string> GetRegionNameAsync(int regionId, CancellationToken cancellationToken = default);

    Task<int> CountBinRangesUsingAsync(int countryId, CancellationToken cancellationToken = default);

    void Add(Country country);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);

    Task<ITransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);
}

public readonly record struct RegionIdentity(int Id, string Name);
