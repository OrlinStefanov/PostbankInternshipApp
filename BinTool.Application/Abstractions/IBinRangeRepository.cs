using BinTool.Application.Models.BinRanges;

namespace BinTool.Application.Abstractions;

public interface IBinRangeRepository
{
    Task<BinRangeListItem?> GetAsync(
        int binRangeId, DateTime today, CancellationToken cancellationToken = default);

    /// <summary>One page of the filtered listing, with the total the pager needs.</summary>
    Task<PagedResult<BinRangeListItem>> SearchAsync(
        BinRangeQuery query, int page, int pageSize, DateTime today,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<BinRangeListItem>> GetManyAsync(
        IReadOnlyCollection<int> binRangeIds, DateTime today,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PrefixScheme>> ListLivePrefixSchemesAsync(
        CancellationToken cancellationToken = default);

    Task<BinRangeFilterOptions> GetFilterOptionsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// The most specific live range covering any of the candidate prefixes on the given day.
    /// </summary>
    Task<MatchedRange?> FindLongestMatchAsync(
        IReadOnlyCollection<string> candidatePrefixes, DateTime onDate,
        CancellationToken cancellationToken = default);

    Task<BinRange?> GetForUpdateAsync(int binRangeId, CancellationToken cancellationToken = default);

    Task<BinRange?> FindByPrefixAsync(string prefix, CancellationToken cancellationToken = default);

    Task<bool> PrefixBelongsToAnotherAsync(
        string prefix, int binRangeId, CancellationToken cancellationToken = default);

    /// <summary>Turns the four names on the input into ids plus their canonical spelling.</summary>
    Task<BinReferenceNames> ResolveByNameAsync(
        string cardScheme, string productType, string fundingType, string countryCode,
        CancellationToken cancellationToken = default);

    void Add(BinRange range);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);

    Task<ITransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);
}
