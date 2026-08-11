using BinTool.Application.Abstractions;
using BinTool.Application.Mapping;
using BinTool.Application.Models.Audit;
using BinTool.Application.Models.Currency;
using BinTool.Application.Models.ReferenceData;
using BinTool.Application.Validation;
using Microsoft.Extensions.Logging;

namespace BinTool.Application.Services;

/// <summary>
/// Maintains the currency table and the euro rates commission is priced in. Every write runs
/// the same beats - validate, refuse a clash, snapshot, apply, audit in the same unit of
/// work, save - and this class does no more than run them in that order.
/// </summary>
public class CurrencyService : ICurrencyService
{
    private readonly ICurrencyRepository _currencies;
    private readonly ICurrentUser _currentUser;
    private readonly IAuditLog _audit;
    private readonly ILogger<CurrencyService> _logger;

    public CurrencyService(
        ICurrencyRepository currencies,
        ICurrentUser currentUser,
        IAuditLog audit,
        ILogger<CurrencyService> logger)
    {
        _currencies = currencies;
        _currentUser = currentUser;
        _audit = audit;
        _logger = logger;
    }

    public async Task<List<CurrencyListItem>> SearchAsync(
        bool includeDeleted = false, CancellationToken cancellationToken = default)
    {
        var rows = await _currencies.ListAsync(includeDeleted, cancellationToken);

        return rows.Select(CurrencyMapper.ToListItem).ToList();
    }

    public async Task<CurrencyListItem?> GetAsync(
        int id, CancellationToken cancellationToken = default)
    {
        var row = await _currencies.GetAsync(id, cancellationToken);

        return row is null ? null : CurrencyMapper.ToListItem(row);
    }

    public async Task<CurrencyMutationResult> CreateAsync(
        CurrencyInput input, CancellationToken cancellationToken = default)
    {
        if (!CurrencyValidator.TryValidate(input, out var error))
        {
            return Refused(CurrencyMutationResult.Failure(LookupMutationStatus.Invalid, error));
        }

        var code = input.NormalizedCode();
        var existing = await _currencies.FindByCodeAsync(code, cancellationToken);

        if (existing is { IsDeleted: false })
        {
            return Refused(CurrencyMutationResult.Failure(
                LookupMutationStatus.NameInUse, $"Currency '{code}' already exists."));
        }

        // The code is unique across deleted rows too, so re-adding one revives its row
        // rather than being refused for a collision the user cannot see.
        if (existing is { IsDeleted: true })
        {
            return await ReviveAsync(existing, input, cancellationToken);
        }

        var now = DateTime.UtcNow;
        var currency = new Currency
        {
            CreatedAt = now,
            CreatedBy = _currentUser.Name,
            UpdatedAt = now,
            UpdatedBy = _currentUser.Name
        };

        CurrencyMapper.Apply(currency, input);

        // An insert has no id until it is saved, and the audit entry has to carry one.
        await using var transaction = await _currencies.BeginTransactionAsync(cancellationToken);

        _currencies.Add(currency);
        await _currencies.SaveChangesAsync(cancellationToken);

        _audit.Record(AuditAction.Created, AuditEntityTypes.Currency, currency.CurrencyId,
            null, CurrencyMapper.ToSnapshot(currency));

        await _currencies.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        CurrencyLog.Created(_logger, currency.CurrencyId, currency.Code, currency.RateToEur, _currentUser.Name);

        return await SucceededAsync(LookupMutationStatus.Created, currency.CurrencyId, cancellationToken);
    }

    public async Task<CurrencyMutationResult> UpdateAsync(
        int id, CurrencyInput input, CancellationToken cancellationToken = default)
    {
        if (!CurrencyValidator.TryValidate(input, out var error))
        {
            return Refused(CurrencyMutationResult.Failure(LookupMutationStatus.Invalid, error), id);
        }

        var current = await _currencies.GetForUpdateAsync(id, cancellationToken);
        if (current is null) return NotFound(id);

        if (current.IsDeleted)
        {
            return Refused(CurrencyMutationResult.Failure(
                LookupMutationStatus.NotFound,
                $"Currency {id} is deleted. Restore it before editing."), id);
        }

        var code = input.NormalizedCode();

        if (!string.Equals(code, current.Code, StringComparison.OrdinalIgnoreCase))
        {
            var taken = await _currencies.FindByCodeAsync(code, cancellationToken);
            if (taken is not null && taken.CurrencyId != id)
            {
                return Refused(CurrencyMutationResult.Failure(
                    LookupMutationStatus.NameInUse, $"Currency '{code}' already exists."), id);
            }
        }

        var before = CurrencyMapper.ToSnapshot(current);
        var previousRate = current.RateToEur;

        CurrencyMapper.Apply(current, input);
        Touch(current);

        _audit.Record(AuditAction.Updated, AuditEntityTypes.Currency, id,
            before, CurrencyMapper.ToSnapshot(current));

        await _currencies.SaveChangesAsync(cancellationToken);

        CurrencyLog.Updated(_logger, id, current.Code, _currentUser.Name);

        // A rate move reprices every rule quoted in this currency at once, which is worth
        // finding in the log later without reconstructing it from the audit diff.
        if (previousRate != current.RateToEur)
        {
            CurrencyLog.RateChanged(_logger, id, current.Code, previousRate, current.RateToEur, _currentUser.Name);
        }

        return await SucceededAsync(LookupMutationStatus.Updated, id, cancellationToken);
    }

    public async Task<CurrencyMutationResult> DeleteAsync(
        int id, CancellationToken cancellationToken = default)
    {
        var current = await _currencies.GetForUpdateAsync(id, cancellationToken);
        if (current is null) return NotFound(id);

        if (current.IsDeleted)
        {
            return Refused(CurrencyMutationResult.Failure(
                LookupMutationStatus.AlreadyInThatState,
                $"Currency '{current.Code}' is already deleted."), id);
        }

        // Refused while a live rule is still priced in it; those amounts would lose the
        // unit they are quoted in.
        var inUse = await _currencies.CountRulesUsingAsync(id, cancellationToken);
        if (inUse > 0)
        {
            return Refused(CurrencyMutationResult.Failure(
                LookupMutationStatus.InUse,
                $"Currency '{current.Code}' is still used by {inUse} commission rule(s). " +
                "Reprice or remove them first."), id);
        }

        var before = CurrencyMapper.ToSnapshot(current);

        current.IsDeleted = true;
        current.DeletedAt = DateTime.UtcNow;
        current.DeletedBy = _currentUser.Name;
        Touch(current);

        _audit.Record(AuditAction.Deleted, AuditEntityTypes.Currency, id,
            before, CurrencyMapper.ToSnapshot(current));

        await _currencies.SaveChangesAsync(cancellationToken);

        CurrencyLog.Deleted(_logger, id, current.Code, _currentUser.Name);

        return await SucceededAsync(LookupMutationStatus.Deleted, id, cancellationToken);
    }

    public async Task<CurrencyMutationResult> RestoreAsync(
        int id, CancellationToken cancellationToken = default)
    {
        var current = await _currencies.GetForUpdateAsync(id, cancellationToken);
        if (current is null) return NotFound(id);

        if (!current.IsDeleted)
        {
            return Refused(CurrencyMutationResult.Failure(
                LookupMutationStatus.AlreadyInThatState,
                $"Currency '{current.Code}' is not deleted."), id);
        }

        var before = CurrencyMapper.ToSnapshot(current);

        current.IsDeleted = false;
        current.DeletedAt = null;
        current.DeletedBy = null;
        Touch(current);

        _audit.Record(AuditAction.Updated, AuditEntityTypes.Currency, id,
            before, CurrencyMapper.ToSnapshot(current));

        await _currencies.SaveChangesAsync(cancellationToken);

        CurrencyLog.Restored(_logger, id, current.Code, _currentUser.Name);

        return await SucceededAsync(LookupMutationStatus.Restored, id, cancellationToken);
    }

    private async Task<CurrencyMutationResult> ReviveAsync(
        Currency existing, CurrencyInput input, CancellationToken cancellationToken)
    {
        var before = CurrencyMapper.ToSnapshot(existing);

        CurrencyMapper.Apply(existing, input);
        existing.IsDeleted = false;
        existing.DeletedAt = null;
        existing.DeletedBy = null;
        Touch(existing);

        _audit.Record(AuditAction.Updated, AuditEntityTypes.Currency, existing.CurrencyId,
            before, CurrencyMapper.ToSnapshot(existing));

        await _currencies.SaveChangesAsync(cancellationToken);

        CurrencyLog.Revived(_logger, existing.CurrencyId, existing.Code, _currentUser.Name);

        return await SucceededAsync(
            LookupMutationStatus.Restored, existing.CurrencyId, cancellationToken);
    }

    private void Touch(Currency currency)
    {
        currency.UpdatedAt = DateTime.UtcNow;
        currency.UpdatedBy = _currentUser.Name;
    }

    private async Task<CurrencyMutationResult> SucceededAsync(
        LookupMutationStatus status, int id, CancellationToken cancellationToken)
    {
        var item = await GetAsync(id, cancellationToken);
        return CurrencyMutationResult.Success(status, item!);
    }

    // Every refused write leaves through here, so the reason logged is the reason the
    // caller was given and the two cannot drift apart.
    private CurrencyMutationResult Refused(CurrencyMutationResult result, int currencyId = 0)
    {
        CurrencyLog.WriteRefused(_logger, currencyId, result.Status.ToString(), result.Error ?? string.Empty);
        return result;
    }

    private CurrencyMutationResult NotFound(int id) =>
        Refused(CurrencyMutationResult.Failure(
            LookupMutationStatus.NotFound, $"No currency with id {id}."), id);
}
