using BinTool.Domain.Common;
using BinTool.Infrastructure.Data;

namespace BinTool.Infrastructure.Repositories;

public class CurrencyRepository : ICurrencyRepository
{
    private readonly AppDbContext _db;

    public CurrencyRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<Currency>> ListAsync(
        bool includeDeleted, CancellationToken cancellationToken = default)
    {
        var query = _db.Currencies.AsNoTracking().AsQueryable();
        if (!includeDeleted) query = query.Where(c => !c.IsDeleted);

        return await query.OrderBy(c => c.Code).ToListAsync(cancellationToken);
    }

    public Task<Currency?> GetAsync(int id, CancellationToken cancellationToken = default) =>
        _db.Currencies.AsNoTracking()
            .FirstOrDefaultAsync(c => c.CurrencyId == id, cancellationToken);

    public Task<Currency?> GetForUpdateAsync(int id, CancellationToken cancellationToken = default) =>
        _db.Currencies.FirstOrDefaultAsync(c => c.CurrencyId == id, cancellationToken);

    public Task<Currency?> FindByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        var upper = code.ToUpperInvariant();

        return _db.Currencies.FirstOrDefaultAsync(c => c.Code == upper, cancellationToken);
    }

    public Task<Currency?> GetLiveAsync(int id, CancellationToken cancellationToken = default) =>
        _db.Currencies.AsNoTracking()
            .FirstOrDefaultAsync(c => c.CurrencyId == id && !c.IsDeleted, cancellationToken);

    public async Task<int?> FindLiveIdByCodeAsync(
        string? code, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(code)) return null;

        var upper = code.Trim().ToUpperInvariant();

        return await _db.Currencies.AsNoTracking()
            .Where(c => !c.IsDeleted && c.Code == upper)
            .Select(c => (int?)c.CurrencyId)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public Task<Currency?> GetBaseCurrencyAsync(CancellationToken cancellationToken = default) =>
        _db.Currencies.AsNoTracking()
            .FirstOrDefaultAsync(
                c => c.Code == DomainConstants.BaseCurrencyCode && !c.IsDeleted, cancellationToken);

    public Task<int> CountRulesUsingAsync(int currencyId, CancellationToken cancellationToken = default) =>
        _db.CommissionRules.AsNoTracking()
            .CountAsync(r => !r.IsDeleted && r.CurrencyId == currencyId, cancellationToken);

    public void Add(Currency currency) => _db.Currencies.Add(currency);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _db.SaveChangesAsync(cancellationToken);

    public async Task<ITransaction> BeginTransactionAsync(CancellationToken cancellationToken = default) =>
        new EfTransaction(await _db.Database.BeginTransactionAsync(cancellationToken));
}
