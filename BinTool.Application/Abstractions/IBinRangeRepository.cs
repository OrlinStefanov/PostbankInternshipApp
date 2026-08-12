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

    /// <summary>
    /// Just enough of every live range to run the detector over it: the scan reads three columns
    /// rather than whole rows, so it stays cheap at tens of thousands of ranges.
    /// </summary>
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

    /// <summary>
    /// Finds a range by prefix across live and soft-deleted rows: the unique index spans both, so a
    /// deleted prefix would otherwise block an insert it should revive.
    /// </summary>
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

public readonly record struct PrefixScheme(int Id, string Prefix, string SchemeName);

public sealed record MatchedRange(
    string Prefix,
    int CardSchemeId,
    string CardScheme,
    int ProductTypeId,
    string ProductType,
    int FundingTypeId,
    string FundingType,
    string CountryCode,
    string CountryName,
    int RegionId,
    string Region,
    DateTime ValidFrom,
    DateTime? ValidTo);

public readonly record struct NamedReference(int Id, string Name);

public readonly record struct BinReferenceNames(
    NamedReference? CardScheme,
    NamedReference? ProductType,
    NamedReference? FundingType,
    NamedReference? Country);
