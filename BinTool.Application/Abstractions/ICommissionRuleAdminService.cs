using BinTool.Application.Models.Commission;

namespace BinTool.Application.Abstractions;

public interface ICommissionRuleAdminService
{
    /// <summary>Lists rules, active ones first, then by specificity and priority.</summary>
    Task<List<CommissionRuleListItem>> SearchAsync(
        bool includeDeleted, bool includeExpired, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns one rule by id, deleted rules included, or null when nothing matches.
    /// </summary>
    Task<CommissionRuleListItem?> GetAsync(int id, CancellationToken cancellationToken = default);

    Task<CommissionRuleMutationResult> CreateAsync(
        CommissionRuleInput input, CancellationToken cancellationToken = default);

    Task<CommissionRuleMutationResult> UpdateAsync(
        int id, CommissionRuleInput input, CancellationToken cancellationToken = default);

    /// <summary>Soft-deletes a rule.</summary>
    Task<CommissionRuleMutationResult> DeleteAsync(int id, CancellationToken cancellationToken = default);

    Task<CommissionRuleMutationResult> RestoreAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>Makes a rule the fallback default, replacing whatever was default before.</summary>
    Task<CommissionRuleMutationResult> SetDefaultAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Clears the configured default, so an unmatched classification produces no fee.
    /// </summary>
    Task<CommissionRuleMutationResult> ClearDefaultAsync(CancellationToken cancellationToken = default);
}
