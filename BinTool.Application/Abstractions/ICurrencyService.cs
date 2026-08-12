using BinTool.Application.Models.Currency;

namespace BinTool.Application.Abstractions;

public interface ICurrencyService
{
    /// <summary>Lists currencies, ordered by code.</summary>
    Task<List<CurrencyListItem>> SearchAsync(
        bool includeDeleted = false, CancellationToken cancellationToken = default);

    /// <summary>Returns one currency by id, deleted rows included.</summary>
    Task<CurrencyListItem?> GetAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>Adds a currency.</summary>
    Task<CurrencyMutationResult> CreateAsync(
        CurrencyInput input, CancellationToken cancellationToken = default);

    /// <summary>Overwrites a currency (name, code, rate, active flag).</summary>
    Task<CurrencyMutationResult> UpdateAsync(
        int id, CurrencyInput input, CancellationToken cancellationToken = default);

    /// <summary>Soft-deletes a currency.</summary>
    Task<CurrencyMutationResult> DeleteAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>Brings a soft-deleted currency back.</summary>
    Task<CurrencyMutationResult> RestoreAsync(int id, CancellationToken cancellationToken = default);
}
