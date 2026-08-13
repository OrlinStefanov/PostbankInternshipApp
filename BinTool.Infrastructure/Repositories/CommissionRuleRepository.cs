using BinTool.Application.Models.Commission;
using BinTool.Domain.Common;
using BinTool.Infrastructure.Data;

namespace BinTool.Infrastructure.Repositories;

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
            .OrderBy(r => r.Priority)
            .FirstOrDefaultAsync(r => r.CommissionRuleId == id, cancellationToken);

    public Task<CommissionRule?> GetForUpdateAsync(
        int id, CancellationToken cancellationToken = default) =>
        _db.CommissionRules
            .Include(r => r.RuleCriteria)
            .OrderBy(r => r.Priority)
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

    public async Task<IReadOnlyList<CommissionRule>> FindMatchingAsync(
        int cardSchemeId, int productTypeId, int fundingTypeId, int regionId, DateTime onDate,
        CancellationToken cancellationToken = default) =>
        await _db.CommissionRules.AsNoTracking()
            .Include(r => r.RuleCriteria)
            .Include(r => r.Currency)
            .Where(r => !r.IsDeleted && r.IsActive)
            .Where(r => r.ValidFrom <= onDate && (r.ValidTo == null || r.ValidTo >= onDate))
            .Where(r => r.RuleCriteria.Any(c =>
                (c.CardSchemeId == null || c.CardSchemeId == cardSchemeId)
                && (c.ProductTypeId == null || c.ProductTypeId == productTypeId)
                && (c.FundingTypeId == null || c.FundingTypeId == fundingTypeId)
                && (c.RegionId == null || c.RegionId == regionId)))
            .OrderBy(r => r.Priority)
            .ToListAsync(cancellationToken);

    public async Task<CommissionRule?> GetLiveDefaultRuleAsync(
        CancellationToken cancellationToken = default)
    {
        var defaultRuleId = await GetDefaultRuleIdAsync(cancellationToken);
        if (defaultRuleId is not { } ruleId) return null;

        return await _db.CommissionRules.AsNoTracking()
            .Include(r => r.RuleCriteria)
            .Include(r => r.Currency)
            .OrderBy(r => r.Priority)
            .FirstOrDefaultAsync(
                r => r.CommissionRuleId == ruleId && !r.IsDeleted, cancellationToken);
    }

    public void Add(CommissionRule rule) => _db.CommissionRules.Add(rule);

    public Task<int?> GetDefaultRuleIdAsync(CancellationToken cancellationToken = default) =>
        _db.DefaultRules.AsNoTracking()
            .OrderBy(d => d.DefaultRuleId)
            .Select(d => (int?)d.CommissionRuleId)
            .FirstOrDefaultAsync(cancellationToken);

    public Task<DefaultRule?> GetDefaultAsync(CancellationToken cancellationToken = default) =>
        _db.DefaultRules
            .OrderBy(d => d.DefaultRuleId)
            .FirstOrDefaultAsync(cancellationToken);

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
