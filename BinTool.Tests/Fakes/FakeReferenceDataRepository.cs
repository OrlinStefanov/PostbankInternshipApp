using BinTool.Domain.Common;

namespace BinTool.Tests.Fakes;

/// <summary>
/// Resolves reference ids from a set of known ones. Anything not registered comes back as a
/// null name, which is how the real repository reports an id that does not exist.
/// </summary>
public sealed class FakeReferenceDataRepository : IReferenceDataRepository
{
    private readonly Dictionary<int, string> _currencies = new() { [1] = "EUR" };
    private readonly Dictionary<int, string> _schemes = new() { [1] = "Visa", [2] = "Mastercard" };
    private readonly Dictionary<int, string> _products = new() { [1] = "Consumer", [2] = "Commercial" };
    private readonly Dictionary<int, string> _funding = new() { [1] = "Credit", [2] = "Debit" };
    private readonly Dictionary<int, string> _regions = new() { [1] = "Domestic", [2] = "International" };

    public Task<ReferenceNames> ResolveAsync(
        int currencyId, RuleCriteriaKey key, CancellationToken cancellationToken = default) =>
        Task.FromResult(new ReferenceNames(
            Name(_currencies, currencyId),
            Name(_schemes, key.CardSchemeId),
            Name(_products, key.ProductTypeId),
            Name(_funding, key.FundingTypeId),
            Name(_regions, key.RegionId)));

    private static string? Name(Dictionary<int, string> known, int? id) =>
        id is { } value && known.TryGetValue(value, out var name) ? name : null;
}
