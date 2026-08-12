using BinTool.Domain.Common;

namespace BinTool.Application.Abstractions;

public readonly record struct ReferenceNames(
    string? Currency,
    string? CardScheme,
    string? ProductType,
    string? FundingType,
    string? Region);

public interface IReferenceDataRepository
{
    /// <summary>
    /// Resolves a currency and a rule key to their names, skipping the wildcards.
    /// </summary>
    Task<ReferenceNames> ResolveAsync(
        int currencyId, RuleCriteriaKey key, CancellationToken cancellationToken = default);
}
