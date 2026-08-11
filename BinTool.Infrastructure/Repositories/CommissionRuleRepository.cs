using BinTool.Domain.Common;
using BinTool.Infrastructure.Data;

namespace BinTool.Infrastructure.Repositories;

// Entity Framework storage for commission rules. Everything about how a rule is fetched - which
// navigations come with it, whether it is tracked, how a date window becomes SQL - is decided here
// and nowhere else.
public class CommissionRuleRepository : ICommissionRuleRepository
{
    private readonly AppDbContext _db;

    public CommissionRuleRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<CommissionRule>> ListAsync(
        bool includeDeleted, CancellationToken cancellationToken = default)
    {
        var query = WithReferences(_db.CommissionRules.AsNoTracking());
        if (!includeDeleted) query = query.Where(r => !r.IsDeleted);

        return await query.ToListAsync(cancellationToken);
    }

    public Task<CommissionRule?> GetWithReferencesAsync(
        int id, CancellationToken cancellationToken = default) =>
        WithReferences(_db.CommissionRules.AsNoTracking())
            .FirstOrDefaultAsync(r => r.CommissionRuleId == id, cancellationToken);

    public Task<CommissionRule?> GetForUpdateAsync(
        int id, CancellationToken cancellationToken = default) =>
        _db.CommissionRules
            .Include(r => r.RuleCriteria)
            .FirstOrDefaultAsync(r => r.CommissionRuleId == id, cancellationToken);

    public async Task<RuleIdentity?> FindKeyOverlapAsync(
        RuleCriteriaKey key, DateRange window, int excludeRuleId,
        CancellationToken cancellationToken = default)
    {
        var conflict = await Overlapping(Live(excludeRuleId), window)
            .Where(r => r.RuleCriteria.Any(c =>
                c.CardSchemeId == key.CardSchemeId
                && c.ProductTypeId == key.ProductTypeId
                && c.FundingTypeId == key.FundingTypeId
                && c.RegionId == key.RegionId))
            .OrderBy(r => r.ValidFrom)
            .Select(r => new { r.CommissionRuleId, r.RuleName })
            .FirstOrDefaultAsync(cancellationToken);

        return conflict is null
            ? null
            : new RuleIdentity(conflict.CommissionRuleId, conflict.RuleName);
    }

    public async Task<IReadOnlyList<CommissionRule>> FindPriorityCandidatesAsync(
        int priority, DateRange window, int excludeRuleId,
        CancellationToken cancellationToken = default) =>
        await Overlapping(Live(excludeRuleId), window)
            .Where(r => r.Priority == priority)
            .Include(r => r.RuleCriteria)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

    public void Add(CommissionRule rule) => _db.CommissionRules.Add(rule);

    public Task<int?> GetDefaultRuleIdAsync(CancellationToken cancellationToken = default) =>
        _db.DefaultRules.AsNoTracking()
            .Select(d => (int?)d.CommissionRuleId)
            .FirstOrDefaultAsync(cancellationToken);

    public Task<DefaultRule?> GetDefaultAsync(CancellationToken cancellationToken = default) =>
        _db.DefaultRules.FirstOrDefaultAsync(cancellationToken);

    public void AddDefault(DefaultRule defaultRule) => _db.DefaultRules.Add(defaultRule);

    public void RemoveDefault(DefaultRule defaultRule) => _db.DefaultRules.Remove(defaultRule);

    public Task<string?> GetRuleNameAsync(int id, CancellationToken cancellationToken = default) =>
        _db.CommissionRules.AsNoTracking()
            .Where(r => r.CommissionRuleId == id)
            .Select(r => r.RuleName)
            .FirstOrDefaultAsync(cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _db.SaveChangesAsync(cancellationToken);

    public async Task<ITransaction> BeginTransactionAsync(
        CancellationToken cancellationToken = default) =>
        new EfTransaction(await _db.Database.BeginTransactionAsync(cancellationToken));

    private IQueryable<CommissionRule> Live(int excludeRuleId) =>
        _db.CommissionRules.Where(r => !r.IsDeleted && r.CommissionRuleId != excludeRuleId);

    // Narrows to rules whose validity touches the window, both ends inclusive and an open-ended
    // rule treated as running forever. The one place this predicate is written: it has to translate
    // to SQL, so Overlaps cannot be called inside the expression - but the two ends still arrive as
    // a DateRange, and every caller gets the same comparison.
    private static IQueryable<CommissionRule> Overlapping(
        IQueryable<CommissionRule> query, DateRange window)
    {
        var from = window.From;
        var to = window.EffectiveTo;

        return query.Where(r => r.ValidFrom <= to && from <= (r.ValidTo ?? DateTime.MaxValue));
    }

    private static IQueryable<CommissionRule> WithReferences(IQueryable<CommissionRule> query) =>
        query
            .Include(r => r.Currency)
            .Include(r => r.RuleCriteria).ThenInclude(c => c.CardScheme)
            .Include(r => r.RuleCriteria).ThenInclude(c => c.ProductType)
            .Include(r => r.RuleCriteria).ThenInclude(c => c.FundingType)
            .Include(r => r.RuleCriteria).ThenInclude(c => c.Region);
}
