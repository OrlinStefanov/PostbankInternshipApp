using BinTool.Domain.Common;

namespace BinTool.Application.Abstractions;

public readonly record struct RuleIdentity(int Id, string Name);

public interface ICommissionRuleRepository
{
    /// <summary>
    /// Every rule with its criteria and the reference rows the key ids point at, so names can be
    /// shown without a second trip.
    /// </summary>
    Task<IReadOnlyList<CommissionRule>> ListAsync(
        bool includeDeleted, CancellationToken cancellationToken = default);

    /// <summary>One rule with its criteria and reference names, untracked.</summary>
    Task<CommissionRule?> GetWithReferencesAsync(
        int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// One rule with its criteria, tracked so edits to it are picked up by the next save.
    /// </summary>
    Task<CommissionRule?> GetForUpdateAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// The first live rule whose key is <em>identical</em> and whose validity touches the given
    /// window.
    /// </summary>
    Task<RuleIdentity?> FindKeyOverlapAsync(
        RuleCriteriaKey key, DateRange window, int excludeRuleId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Live rules sharing the given priority whose validity touches the window, with their criteria
    /// loaded.
    /// </summary>
    Task<IReadOnlyList<CommissionRule>> FindPriorityCandidatesAsync(
        int priority, DateRange window, int excludeRuleId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Every live rule whose criteria match the card and whose validity covers the day, with its
    /// criteria and currency loaded.
    /// </summary>
    Task<IReadOnlyList<CommissionRule>> FindMatchingAsync(
        int cardSchemeId, int productTypeId, int fundingTypeId, int regionId, DateTime onDate,
        CancellationToken cancellationToken = default);

    /// <summary>The rule serving as fallback, with its criteria and currency loaded.</summary>
    Task<CommissionRule?> GetLiveDefaultRuleAsync(CancellationToken cancellationToken = default);

    void Add(CommissionRule rule);

    /// <summary>The id of the rule currently serving as fallback default, if any.</summary>
    Task<int?> GetDefaultRuleIdAsync(CancellationToken cancellationToken = default);

    /// <summary>The default-rule row itself, tracked, or null when no default is set.</summary>
    Task<DefaultRule?> GetDefaultAsync(CancellationToken cancellationToken = default);

    void AddDefault(DefaultRule defaultRule);

    void RemoveDefault(DefaultRule defaultRule);

    /// <summary>A rule's name without loading the rest of it.</summary>
    Task<string?> GetRuleNameAsync(int id, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);

    Task<ITransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);
}
