using BinTool.Core.Models.Commission;

namespace BinTool.Core.Services;

/// <summary>
/// Resolves the commission for a classified card. Given the card's key attributes and an
/// amount, it selects the most specific rule that matches and is valid on the date, works
/// out the fee, and falls back to the configured default when nothing matches.
/// </summary>
public interface ICommissionResolver
{
    /// <summary>
    /// Works out the fee for a transaction.
    /// </summary>
    /// <param name="cardSchemeId">The card's scheme id.</param>
    /// <param name="productTypeId">The card's product type id.</param>
    /// <param name="regionId">The issuing country's region id.</param>
    /// <param name="amount">The transaction amount.</param>
    /// <param name="onDate">The date the transaction is priced on.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// The calculation, or null when no rule matched and no default is configured - there is
    /// simply no price to quote, which the caller renders as an "unknown pricing" state.
    /// </returns>
    Task<CommissionCalculation?> ResolveAsync(
        int cardSchemeId,
        int productTypeId,
        int regionId,
        decimal amount,
        DateTime onDate,
        CancellationToken cancellationToken = default);
}
