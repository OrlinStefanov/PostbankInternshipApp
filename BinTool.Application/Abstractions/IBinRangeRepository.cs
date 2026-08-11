using BinTool.Application.Models.BinRanges;

namespace BinTool.Application.Abstractions;

/// <summary>
/// Storage for BIN ranges, serving both the admin writes and the browse reads.
/// <para>
/// Reads come back as <see cref="BinRangeListItem"/> rather than as entities. The listing
/// resolves four navigations and derives a status, and doing that in the database is the
/// difference between one query and one per row - so the projection has to be an expression
/// the provider can translate, which means it belongs on this side of the boundary. Writes
/// still work on the entity, because a write has to change stored state.
/// </para>
/// </summary>
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
    /// Just enough of every live range to run the detector over it: the scan reads three
    /// columns rather than whole rows, so it stays cheap at tens of thousands of ranges.
    /// </summary>
    Task<IReadOnlyList<PrefixScheme>> ListLivePrefixSchemesAsync(
        CancellationToken cancellationToken = default);

    Task<BinRangeFilterOptions> GetFilterOptionsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// The most specific live range covering any of the candidate prefixes on the given day.
    /// One query for all three candidate lengths, longest first: an 8-digit range is more
    /// specific than the 6-digit range it sits inside, so it wins. The key ids come back
    /// alongside the names so a fee can be resolved without a second lookup.
    /// </summary>
    Task<MatchedRange?> FindLongestMatchAsync(
        IReadOnlyCollection<string> candidatePrefixes, DateTime onDate,
        CancellationToken cancellationToken = default);

    Task<BinRange?> GetForUpdateAsync(int binRangeId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds a range by prefix across live and soft-deleted rows: the unique index spans
    /// both, so a deleted prefix would otherwise block an insert it should revive.
    /// </summary>
    Task<BinRange?> FindByPrefixAsync(string prefix, CancellationToken cancellationToken = default);

    Task<bool> PrefixBelongsToAnotherAsync(
        string prefix, int binRangeId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Turns the four names on the input into ids plus their canonical spelling. Any that
    /// does not resolve to a live row comes back null, so the caller can name all of them
    /// at once rather than one refusal at a time.
    /// </summary>
    Task<BinReferenceNames> ResolveByNameAsync(
        string cardScheme, string productType, string fundingType, string countryCode,
        CancellationToken cancellationToken = default);

    void Add(BinRange range);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);

    Task<ITransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);
}

/// <summary>A live range reduced to what the scheme detector needs to judge it.</summary>
public readonly record struct PrefixScheme(int Id, string Prefix, string SchemeName);

/// <summary>
/// The range a BIN classified to. Carries both the names, which the answer shows, and the
/// key ids, which pricing needs - so a classification that also prices costs one query.
/// </summary>
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

/// <summary>A reference row that resolved: its id and the canonical spelling of its name.</summary>
public readonly record struct NamedReference(int Id, string Name);

/// <summary>
/// The four references a BIN range names. A null member is one that did not resolve to a
/// live row.
/// </summary>
public readonly record struct BinReferenceNames(
    NamedReference? CardScheme,
    NamedReference? ProductType,
    NamedReference? FundingType,
    NamedReference? Country);
