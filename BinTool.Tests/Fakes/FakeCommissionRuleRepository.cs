using BinTool.Domain.Common;

namespace BinTool.Tests.Fakes;

// A commission rule store held in a list. Enough of the real repository's behaviour to run the
// service against - ids handed out on add, soft-deleted rules skipped, the same inclusive
// date-overlap arithmetic - and none of the database.
public sealed class FakeCommissionRuleRepository : ICommissionRuleRepository
{
    private readonly List<CommissionRule> _rules = new();
    private DefaultRule? _default;
    private int _nextRuleId = 1;

    public int SaveCount { get; private set; }

    public int TransactionsStarted { get; private set; }

    public int TransactionsCommitted { get; private set; }

    public CommissionRule Seed(
        string name,
        int priority = 0,
        int priorityScore = 0,
        int? scheme = null,
        int? product = null,
        int? funding = null,
        int? region = null,
        DateTime? validFrom = null,
        DateTime? validTo = null,
        bool isDeleted = false,
        bool isActive = true)
    {
        var rule = new CommissionRule
        {
            CommissionRuleId = _nextRuleId++,
            RuleName = name,
            Priority = priority,
            ValidFrom = validFrom ?? new DateTime(2025, 1, 1),
            ValidTo = validTo,
            IsActive = isActive,
            IsDeleted = isDeleted,
            RuleCriteria =
            {
                new RuleCriteria
                {
                    CardSchemeId = scheme,
                    ProductTypeId = product,
                    FundingTypeId = funding,
                    RegionId = region,
                    PriorityScore = priorityScore
                }
            }
        };

        _rules.Add(rule);
        return rule;
    }

    public CommissionRule? Find(int id) => _rules.FirstOrDefault(r => r.CommissionRuleId == id);

    // ---- ICommissionRuleRepository ---------------------------------------------

    public Task<IReadOnlyList<CommissionRule>> ListAsync(
        bool includeDeleted, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<CommissionRule>>(
            _rules.Where(r => includeDeleted || !r.IsDeleted).ToList());

    public Task<CommissionRule?> GetWithReferencesAsync(
        int id, CancellationToken cancellationToken = default) =>
        Task.FromResult(Find(id));

    public Task<CommissionRule?> GetForUpdateAsync(
        int id, CancellationToken cancellationToken = default) =>
        Task.FromResult(Find(id));

    public Task<RuleIdentity?> FindKeyOverlapAsync(
        RuleCriteriaKey key, DateRange window, int excludeRuleId,
        CancellationToken cancellationToken = default)
    {
        var match = Candidates(window, excludeRuleId)
            .Where(r => r.Key() == key)
            .OrderBy(r => r.ValidFrom)
            .FirstOrDefault();

        return Task.FromResult<RuleIdentity?>(
            match is null ? null : new RuleIdentity(match.CommissionRuleId, match.RuleName));
    }

    public Task<IReadOnlyList<CommissionRule>> FindPriorityCandidatesAsync(
        int priority, DateRange window, int excludeRuleId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<CommissionRule>>(
            Candidates(window, excludeRuleId).Where(r => r.Priority == priority).ToList());

    public Task<IReadOnlyList<CommissionRule>> FindMatchingAsync(
        int cardSchemeId, int productTypeId, int fundingTypeId, int regionId, DateTime onDate,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<CommissionRule>>(
            _rules.Where(r =>
                !r.IsDeleted
                && r.IsActive
                && r.Validity().Contains(onDate)
                && r.RuleCriteria.Any(c =>
                    (c.CardSchemeId is null || c.CardSchemeId == cardSchemeId)
                    && (c.ProductTypeId is null || c.ProductTypeId == productTypeId)
                    && (c.FundingTypeId is null || c.FundingTypeId == fundingTypeId)
                    && (c.RegionId is null || c.RegionId == regionId)))
                .ToList());

    public Task<CommissionRule?> GetLiveDefaultRuleAsync(
        CancellationToken cancellationToken = default) =>
        Task.FromResult(_default is null ? null : Find(_default.CommissionRuleId) is { IsDeleted: false } rule ? rule : null);

    public void Add(CommissionRule rule)
    {
        rule.CommissionRuleId = _nextRuleId++;
        _rules.Add(rule);
    }

    public Task<int?> GetDefaultRuleIdAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(_default?.CommissionRuleId);

    public Task<DefaultRule?> GetDefaultAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(_default);

    public void AddDefault(DefaultRule defaultRule)
    {
        defaultRule.DefaultRuleId = 1;
        _default = defaultRule;
    }

    public void RemoveDefault(DefaultRule defaultRule) => _default = null;

    public Task<string?> GetRuleNameAsync(int id, CancellationToken cancellationToken = default) =>
        Task.FromResult(Find(id)?.RuleName);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        SaveCount++;
        return Task.CompletedTask;
    }

    public Task<ITransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        TransactionsStarted++;
        return Task.FromResult<ITransaction>(new FakeTransaction(this));
    }

    private IEnumerable<CommissionRule> Candidates(DateRange window, int excludeRuleId) =>
        _rules.Where(r =>
            !r.IsDeleted
            && r.CommissionRuleId != excludeRuleId
            && r.Validity().Overlaps(window));

    private sealed class FakeTransaction : ITransaction
    {
        private readonly FakeCommissionRuleRepository _owner;

        public FakeTransaction(FakeCommissionRuleRepository owner)
        {
            _owner = owner;
        }

        public Task CommitAsync(CancellationToken cancellationToken = default)
        {
            _owner.TransactionsCommitted++;
            return Task.CompletedTask;
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
