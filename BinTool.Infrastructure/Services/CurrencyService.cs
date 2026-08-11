using System.ComponentModel.DataAnnotations;
using BinTool.Domain.Entities;
using BinTool.Application.Models.Audit;
using BinTool.Application.Models.Currency;
using BinTool.Application.Models.ReferenceData;
using BinTool.Application.Abstractions;
using BinTool.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BinTool.Infrastructure.Services;

/// <summary>
/// Maintains the currency table and the euro rates used to price and display commission.
/// Mirrors <see cref="LookupAdminService"/> - validate, snapshot, apply, audit in the same
/// unit of work, save - but stands alone because a currency carries a code and a rate rather
/// than the shared name+description shape. The code is unique case-insensitively across live
/// and soft-deleted rows, so a deleted code revives its row rather than duplicating it.
/// </summary>
public class CurrencyService : ICurrencyService
{
    private readonly AppDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly IAuditLog _audit;

    public CurrencyService(AppDbContext db, ICurrentUser currentUser, IAuditLog audit)
    {
        _db = db;
        _currentUser = currentUser;
        _audit = audit;
    }

    public async Task<List<CurrencyListItem>> SearchAsync(
        bool includeDeleted = false, CancellationToken cancellationToken = default)
    {
        var query = _db.Currencies.AsNoTracking().AsQueryable();
        if (!includeDeleted) query = query.Where(c => !c.IsDeleted);

        var rows = await query.OrderBy(c => c.Code).ToListAsync(cancellationToken);

        return rows.Select(ToListItem).ToList();
    }

    public async Task<CurrencyListItem?> GetAsync(
        int id, CancellationToken cancellationToken = default)
    {
        var row = await _db.Currencies.AsNoTracking()
            .FirstOrDefaultAsync(c => c.CurrencyId == id, cancellationToken);

        return row is null ? null : ToListItem(row);
    }

    public async Task<CurrencyMutationResult> CreateAsync(
        CurrencyInput input, CancellationToken cancellationToken = default)
    {
        if (!TryValidate(input, out var invalid)) return invalid;

        var code = input.Code.Trim().ToUpperInvariant();
        var name = input.Name.Trim();

        var existing = await FindByCodeAsync(code, cancellationToken);

        if (existing is { IsDeleted: false })
        {
            return CurrencyMutationResult.Failure(
                LookupMutationStatus.NameInUse, $"Currency '{code}' already exists.");
        }

        if (existing is { IsDeleted: true })
        {
            var before = Snapshot(existing);

            existing.Code = code;
            existing.Name = name;
            existing.RateToEur = input.RateToEur;
            existing.IsActive = input.IsActive;
            existing.IsDeleted = false;
            existing.DeletedAt = null;
            existing.DeletedBy = null;
            Touch(existing);

            _audit.Record(AuditAction.Updated, AuditEntityTypes.Currency, existing.CurrencyId,
                before, Snapshot(existing));

            await _db.SaveChangesAsync(cancellationToken);

            return await SucceededAsync(LookupMutationStatus.Restored, existing.CurrencyId, cancellationToken);
        }

        var now = DateTime.UtcNow;
        var currency = new Currency
        {
            Code = code,
            Name = name,
            RateToEur = input.RateToEur,
            IsActive = input.IsActive,
            CreatedAt = now,
            CreatedBy = _currentUser.Name,
            UpdatedAt = now,
            UpdatedBy = _currentUser.Name
        };

        // An insert has no id until it is saved, and the audit entry has to carry one.
        await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);

        _db.Currencies.Add(currency);
        await _db.SaveChangesAsync(cancellationToken);

        _audit.Record(AuditAction.Created, AuditEntityTypes.Currency, currency.CurrencyId,
            null, Snapshot(currency));

        await _db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return await SucceededAsync(LookupMutationStatus.Created, currency.CurrencyId, cancellationToken);
    }

    public async Task<CurrencyMutationResult> UpdateAsync(
        int id, CurrencyInput input, CancellationToken cancellationToken = default)
    {
        if (!TryValidate(input, out var invalid)) return invalid;

        var current = await _db.Currencies.FirstOrDefaultAsync(c => c.CurrencyId == id, cancellationToken);
        if (current is null) return NotFound(id);

        if (current.IsDeleted)
        {
            return CurrencyMutationResult.Failure(
                LookupMutationStatus.NotFound,
                $"Currency {id} is deleted. Restore it before editing.");
        }

        var code = input.Code.Trim().ToUpperInvariant();
        var name = input.Name.Trim();

        if (!string.Equals(code, current.Code, StringComparison.OrdinalIgnoreCase))
        {
            var taken = await FindByCodeAsync(code, cancellationToken);
            if (taken is not null && taken.CurrencyId != id)
            {
                return CurrencyMutationResult.Failure(
                    LookupMutationStatus.NameInUse, $"Currency '{code}' already exists.");
            }
        }

        var before = Snapshot(current);

        current.Code = code;
        current.Name = name;
        current.RateToEur = input.RateToEur;
        current.IsActive = input.IsActive;
        Touch(current);

        _audit.Record(AuditAction.Updated, AuditEntityTypes.Currency, id, before, Snapshot(current));

        await _db.SaveChangesAsync(cancellationToken);

        return await SucceededAsync(LookupMutationStatus.Updated, id, cancellationToken);
    }

    public async Task<CurrencyMutationResult> DeleteAsync(
        int id, CancellationToken cancellationToken = default)
    {
        var current = await _db.Currencies.FirstOrDefaultAsync(c => c.CurrencyId == id, cancellationToken);
        if (current is null) return NotFound(id);

        if (current.IsDeleted)
        {
            return CurrencyMutationResult.Failure(
                LookupMutationStatus.AlreadyInThatState,
                $"Currency '{current.Code}' is already deleted.");
        }

        // Deletion is refused while a live commission rule is still priced in this currency;
        // its amounts would lose the unit they are quoted in.
        var inUse = await _db.CommissionRules.AsNoTracking()
            .CountAsync(r => !r.IsDeleted && r.CurrencyId == id, cancellationToken);
        if (inUse > 0)
        {
            return CurrencyMutationResult.Failure(
                LookupMutationStatus.InUse,
                $"Currency '{current.Code}' is still used by {inUse} commission rule(s). " +
                "Reprice or remove them first.");
        }

        var before = Snapshot(current);

        current.IsDeleted = true;
        current.DeletedAt = DateTime.UtcNow;
        current.DeletedBy = _currentUser.Name;
        Touch(current);

        _audit.Record(AuditAction.Deleted, AuditEntityTypes.Currency, id, before, Snapshot(current));

        await _db.SaveChangesAsync(cancellationToken);

        return await SucceededAsync(LookupMutationStatus.Deleted, id, cancellationToken);
    }

    public async Task<CurrencyMutationResult> RestoreAsync(
        int id, CancellationToken cancellationToken = default)
    {
        var current = await _db.Currencies.FirstOrDefaultAsync(c => c.CurrencyId == id, cancellationToken);
        if (current is null) return NotFound(id);

        if (!current.IsDeleted)
        {
            return CurrencyMutationResult.Failure(
                LookupMutationStatus.AlreadyInThatState,
                $"Currency '{current.Code}' is not deleted.");
        }

        var before = Snapshot(current);

        current.IsDeleted = false;
        current.DeletedAt = null;
        current.DeletedBy = null;
        Touch(current);

        _audit.Record(AuditAction.Updated, AuditEntityTypes.Currency, id, before, Snapshot(current));

        await _db.SaveChangesAsync(cancellationToken);

        return await SucceededAsync(LookupMutationStatus.Restored, id, cancellationToken);
    }

    // ---- Helpers --------------------------------------------------------------

    private Task<Currency?> FindByCodeAsync(string code, CancellationToken cancellationToken)
    {
        var upper = code.ToUpperInvariant();
        return _db.Currencies.FirstOrDefaultAsync(c => c.Code == upper, cancellationToken);
    }

    private void Touch(Currency currency)
    {
        currency.UpdatedAt = DateTime.UtcNow;
        currency.UpdatedBy = _currentUser.Name;
    }

    private static CurrencySnapshot Snapshot(Currency c) =>
        new(c.Code, c.Name, c.RateToEur, c.IsActive, c.IsDeleted);

    private static CurrencyListItem ToListItem(Currency c) => new()
    {
        Id = c.CurrencyId,
        Code = c.Code,
        Name = c.Name,
        RateToEur = c.RateToEur,
        IsActive = c.IsActive,
        Status = c.IsDeleted ? LookupStatus.Deleted : LookupStatus.Active,
        DeletedAt = c.DeletedAt,
        DeletedBy = c.DeletedBy
    };

    private async Task<CurrencyMutationResult> SucceededAsync(
        LookupMutationStatus status, int id, CancellationToken cancellationToken)
    {
        var item = await GetAsync(id, cancellationToken);
        return CurrencyMutationResult.Success(status, item!);
    }

    private static CurrencyMutationResult NotFound(int id) =>
        CurrencyMutationResult.Failure(LookupMutationStatus.NotFound, $"No currency with id {id}.");

    private static bool TryValidate(CurrencyInput input, out CurrencyMutationResult failure)
    {
        var results = new List<ValidationResult>();

        if (Validator.TryValidateObject(
                input, new ValidationContext(input), results, validateAllProperties: true))
        {
            failure = null!;
            return true;
        }

        failure = CurrencyMutationResult.Failure(
            LookupMutationStatus.Invalid, string.Join(" ", results.Select(r => r.ErrorMessage)));

        return false;
    }
}
