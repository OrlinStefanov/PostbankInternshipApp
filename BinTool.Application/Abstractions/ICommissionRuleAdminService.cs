using BinTool.Application.Models.Commission;

namespace BinTool.Application.Abstractions;

/// <summary>
/// Maintenance surface for commission rules: the pricing an admin sets so a tariff change
/// needs no developer and no release. Every write records who made it and refuses a save
/// that would overlap an existing rule on the same scheme/product/region key.
/// </summary>
public interface ICommissionRuleAdminService
{
    /// <summary>
    /// Lists rules, active ones first, then by specificity and priority.
    /// </summary>
    /// <param name="includeDeleted">When true, soft-deleted rules are included.</param>
    /// <param name="includeExpired">When true, expired rules are included.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<List<CommissionRuleListItem>> SearchAsync(
        bool includeDeleted, bool includeExpired, CancellationToken cancellationToken = default);

    /// <summary>Returns one rule by id, deleted rules included, or null when nothing matches.</summary>
    Task<CommissionRuleListItem?> GetAsync(int id, CancellationToken cancellationToken = default);

    Task<CommissionRuleMutationResult> CreateAsync(
        CommissionRuleInput input, CancellationToken cancellationToken = default);

    Task<CommissionRuleMutationResult> UpdateAsync(
        int id, CommissionRuleInput input, CancellationToken cancellationToken = default);

    /// <summary>Soft-deletes a rule. Refused while the rule is the configured default.</summary>
    Task<CommissionRuleMutationResult> DeleteAsync(int id, CancellationToken cancellationToken = default);

    Task<CommissionRuleMutationResult> RestoreAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Makes a rule the fallback default, replacing whatever was default before. Refused
    /// when the target rule is soft-deleted or inactive.
    /// </summary>
    Task<CommissionRuleMutationResult> SetDefaultAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Clears the configured default, so an unmatched classification produces no fee. A no-op
    /// that still succeeds when there was no default to begin with.
    /// </summary>
    Task<CommissionRuleMutationResult> ClearDefaultAsync(CancellationToken cancellationToken = default);
}
