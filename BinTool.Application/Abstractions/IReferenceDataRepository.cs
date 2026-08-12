using BinTool.Application.Models.ReferenceData;
using BinTool.Domain.Common;

namespace BinTool.Application.Abstractions;

public interface IReferenceDataRepository
{
    /// <summary>
    /// Resolves a currency and a rule key to their names, skipping the wildcards.
    /// </summary>
    Task<ReferenceNames> ResolveAsync(
        int currencyId, RuleCriteriaKey key, CancellationToken cancellationToken = default);
}
