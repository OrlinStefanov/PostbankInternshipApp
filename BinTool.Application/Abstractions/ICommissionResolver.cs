using BinTool.Application.Models.Commission;

namespace BinTool.Application.Abstractions;

public interface ICommissionResolver
{
    /// <summary>Works out the fee for a transaction.</summary>
    Task<CommissionCalculation?> ResolveAsync(
        int cardSchemeId,
        int productTypeId,
        int fundingTypeId,
        int regionId,
        decimal amount,
        DateTime onDate,
        int? inputCurrencyId = null,
        CancellationToken cancellationToken = default);
}
