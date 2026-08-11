using BinTool.Domain.Common;

namespace BinTool.Application.Abstractions;

/// <summary>
/// The canonical names behind a set of reference ids. A null name means the id did not
/// resolve to a live row - so a caller tells "not asked for" from "asked for and missing"
/// by comparing against the ids it supplied, rather than this type having to encode both.
/// </summary>
public readonly record struct ReferenceNames(
    string? Currency,
    string? CardScheme,
    string? ProductType,
    string? FundingType,
    string? Region);

public interface IReferenceDataRepository
{
    /// <summary>
    /// Resolves a currency and a rule key to their names, skipping the wildcards. Only live
    /// reference rows are considered, so a retired scheme cannot be assigned to a new rule.
    /// </summary>
    Task<ReferenceNames> ResolveAsync(
        int currencyId, RuleCriteriaKey key, CancellationToken cancellationToken = default);
}
