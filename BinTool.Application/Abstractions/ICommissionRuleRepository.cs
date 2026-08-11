using BinTool.Domain.Common;

namespace BinTool.Application.Abstractions;

/// <summary>Just enough of a rule to name it in a refusal.</summary>
public readonly record struct RuleIdentity(int Id, string Name);

/// <summary>
/// Storage for commission rules and the fallback default.
/// <para>
/// Every method hands back a materialized result - an entity, a list, a value. None returns
/// a query for the caller to go on building, because a caller that can go on building the
/// query is a caller that decides what SQL runs, and then this interface would be describing
/// nothing.
/// </para>
/// </summary>
public interface ICommissionRuleRepository
{
    /// <summary>
    /// Every rule with its criteria and the reference rows the key ids point at, so names
    /// can be shown without a second trip. Untracked.
    /// </summary>
    Task<IReadOnlyList<CommissionRule>> ListAsync(
        bool includeDeleted, CancellationToken cancellationToken = default);

    /// <summary>One rule with its criteria and reference names, untracked. Null when absent.</summary>
    Task<CommissionRule?> GetWithReferencesAsync(
        int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// One rule with its criteria, tracked so edits to it are picked up by the next save.
    /// </summary>
    Task<CommissionRule?> GetForUpdateAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// The first live rule whose key is <em>identical</em> and whose validity touches the
    /// given window. Two rules with the same key covering the same days are duplicate
    /// tariffs whatever their priority, which is a separate question from ambiguity.
    /// </summary>
    Task<RuleIdentity?> FindKeyOverlapAsync(
        RuleCriteriaKey key, DateRange window, int excludeRuleId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Live rules sharing the given priority whose validity touches the window, with their
    /// criteria loaded. The narrowing that is left - equal score, co-matchable keys - is a
    /// domain rule, so it is settled by the caller against <see cref="RuleCriteriaKey"/>
    /// rather than expressed as SQL.
    /// </summary>
    Task<IReadOnlyList<CommissionRule>> FindPriorityCandidatesAsync(
        int priority, DateRange window, int excludeRuleId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Every live rule whose criteria match the card and whose validity covers the day, with
    /// its criteria and currency loaded. A null criteria field is a wildcard and matches
    /// anything. The set is small by design, so the ranking between them is settled by the
    /// caller rather than pushed into an ORDER BY that would hide the tiebreak.
    /// </summary>
    Task<IReadOnlyList<CommissionRule>> FindMatchingAsync(
        int cardSchemeId, int productTypeId, int fundingTypeId, int regionId, DateTime onDate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// The rule serving as fallback, with its criteria and currency loaded. Null when no
    /// default is configured, and also when the configured one has since been deleted - a
    /// default pointing at a deleted rule is no default at all.
    /// </summary>
    Task<CommissionRule?> GetLiveDefaultRuleAsync(CancellationToken cancellationToken = default);

    void Add(CommissionRule rule);

    /// <summary>The id of the rule currently serving as fallback default, if any.</summary>
    Task<int?> GetDefaultRuleIdAsync(CancellationToken cancellationToken = default);

    /// <summary>The default-rule row itself, tracked, or null when no default is set.</summary>
    Task<DefaultRule?> GetDefaultAsync(CancellationToken cancellationToken = default);

    void AddDefault(DefaultRule defaultRule);

    void RemoveDefault(DefaultRule defaultRule);

    /// <summary>A rule's name without loading the rest of it. Null when the rule is absent.</summary>
    Task<string?> GetRuleNameAsync(int id, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);

    Task<ITransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);
}
