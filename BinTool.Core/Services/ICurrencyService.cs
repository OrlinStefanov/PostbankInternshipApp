using BinTool.Core.Models.Currency;

namespace BinTool.Core.Services;

/// <summary>
/// Maintains the currency table and the euro rates used to price and display commission.
/// Follows the same beats as the reference-data admin services: validate, snapshot, apply,
/// audit in the same unit of work, and save.
/// </summary>
public interface ICurrencyService
{
    /// <summary>Lists currencies, ordered by code. Soft-deleted rows only when asked.</summary>
    Task<List<CurrencyListItem>> SearchAsync(
        bool includeDeleted = false, CancellationToken cancellationToken = default);

    /// <summary>Returns one currency by id, deleted rows included. Null if none has that id.</summary>
    Task<CurrencyListItem?> GetAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a currency. A code already held by a soft-deleted row revives that row in place.
    /// </summary>
    Task<CurrencyMutationResult> CreateAsync(
        CurrencyInput input, CancellationToken cancellationToken = default);

    /// <summary>Overwrites a currency (name, code, rate, active flag).</summary>
    Task<CurrencyMutationResult> UpdateAsync(
        int id, CurrencyInput input, CancellationToken cancellationToken = default);

    /// <summary>
    /// Soft-deletes a currency. Refused while any live commission rule still uses it.
    /// </summary>
    Task<CurrencyMutationResult> DeleteAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>Brings a soft-deleted currency back.</summary>
    Task<CurrencyMutationResult> RestoreAsync(int id, CancellationToken cancellationToken = default);
}
