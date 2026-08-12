using BinTool.Application.Models.ReferenceData;

namespace BinTool.Application.Abstractions;

public interface ICountryRepository
{
    /// <summary>Countries with their region loaded, ordered by ISO code.</summary>
    Task<IReadOnlyList<Country>> ListAsync(
        bool includeDeleted, CancellationToken cancellationToken = default);

    Task<Country?> GetWithRegionAsync(int id, CancellationToken cancellationToken = default);

    Task<Country?> GetForUpdateAsync(int id, CancellationToken cancellationToken = default);

    Task<Country?> FindByIsoCodeAsync(string isoCode, CancellationToken cancellationToken = default);

    /// <summary>A live region by id, or null.</summary>
    Task<RegionIdentity?> FindLiveRegionAsync(
        int regionId, CancellationToken cancellationToken = default);

    /// <summary>The stored name of any region, deleted included, for a before-snapshot.</summary>
    Task<string> GetRegionNameAsync(int regionId, CancellationToken cancellationToken = default);

    Task<int> CountBinRangesUsingAsync(int countryId, CancellationToken cancellationToken = default);

    void Add(Country country);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);

    Task<ITransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);
}
