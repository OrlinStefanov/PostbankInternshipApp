using BinTool.Application.Abstractions;
using BinTool.Application.Mapping;
using BinTool.Application.Models.Audit;
using BinTool.Application.Models.ReferenceData;
using BinTool.Application.Validation;
using Microsoft.Extensions.Logging;

namespace BinTool.Application.Services;

/// <summary>
/// The four Name+Description reference tables - card scheme, product type, funding type and
/// region - share a shape, so one service serves all four. The <see cref="LookupKind"/> only
/// picks the table and the audit entity-type constant; every rule below is identical.
/// </summary>
public class LookupAdminService : ILookupAdminService
{
    private readonly ILookupRepository _lookups;
    private readonly ICurrentUser _currentUser;
    private readonly IAuditLog _audit;
    private readonly ILogger<LookupAdminService> _logger;

    public LookupAdminService(
        ILookupRepository lookups,
        ICurrentUser currentUser,
        IAuditLog audit,
        ILogger<LookupAdminService> logger)
    {
        _lookups = lookups;
        _currentUser = currentUser;
        _audit = audit;
        _logger = logger;
    }

    public async Task<List<LookupListItem>> SearchAsync(
        LookupKind kind, bool includeDeleted, CancellationToken cancellationToken = default)
    {
        var rows = await _lookups.ListAsync(kind, includeDeleted, cancellationToken);

        return rows.Select(LookupMapper.ToListItem).ToList();
    }

    public async Task<LookupListItem?> GetAsync(
        LookupKind kind, int id, CancellationToken cancellationToken = default)
    {
        var row = await _lookups.GetAsync(kind, id, cancellationToken);

        return row is null ? null : LookupMapper.ToListItem(row);
    }

    public async Task<LookupMutationResult> CreateAsync(
        LookupKind kind, LookupInput input, CancellationToken cancellationToken = default)
    {
        if (!LookupValidator.TryValidate(input, out var error))
        {
            return Refused(kind, LookupMutationResult.Failure(LookupMutationStatus.Invalid, error));
        }

        var name = input.NormalizedName();
        var existing = await _lookups.FindByNameAsync(kind, name, cancellationToken);

        if (existing is { IsDeleted: false })
        {
            return Refused(kind, LookupMutationResult.Failure(
                LookupMutationStatus.NameInUse,
                $"{LookupMapper.DisplayName(kind)} '{name}' already exists."));
        }

        // The unique index on Name spans soft-deleted rows, so re-adding a deleted name
        // revives that row: it keeps its id, its foreign keys and its audit trail.
        if (existing is { IsDeleted: true })
        {
            return await ReviveAsync(kind, existing, input, cancellationToken);
        }

        // An insert has no id until it is saved, and the audit entry has to carry one.
        await using var transaction = await _lookups.BeginTransactionAsync(cancellationToken);

        var created = _lookups.Add(kind, name, input.NormalizedDescription());
        await _lookups.SaveChangesAsync(cancellationToken);

        _audit.Record(AuditAction.Created, LookupMapper.EntityTypeFor(kind), created.Id,
            null, LookupMapper.ToSnapshot(created));

        await _lookups.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        LookupLog.Created(_logger, kind.ToString(), created.Id, name, _currentUser.Name);

        return await SucceededAsync(kind, LookupMutationStatus.Created, created.Id, cancellationToken);
    }

    public async Task<LookupMutationResult> UpdateAsync(
        LookupKind kind, int id, LookupInput input, CancellationToken cancellationToken = default)
    {
        if (!LookupValidator.TryValidate(input, out var error))
        {
            return Refused(kind, LookupMutationResult.Failure(
                LookupMutationStatus.Invalid, error), id);
        }

        var current = await _lookups.GetForUpdateAsync(kind, id, cancellationToken);
        if (current is null) return NotFound(kind, id);

        if (current.IsDeleted)
        {
            // Editing a deleted row would quietly resurrect it as a side effect.
            return Refused(kind, LookupMutationResult.Failure(
                LookupMutationStatus.NotFound,
                $"{LookupMapper.DisplayName(kind)} {id} is deleted. Restore it before editing."), id);
        }

        var name = input.NormalizedName();

        if (!string.Equals(name, current.Name, StringComparison.OrdinalIgnoreCase))
        {
            var taken = await _lookups.FindByNameAsync(kind, name, cancellationToken);
            if (taken is not null && taken.Id != id)
            {
                return Refused(kind, LookupMutationResult.Failure(
                    LookupMutationStatus.NameInUse,
                    $"{LookupMapper.DisplayName(kind)} '{name}' already exists."), id);
            }
        }

        var before = LookupMapper.ToSnapshot(current);

        LookupMapper.Apply(current, input);

        _audit.Record(AuditAction.Updated, LookupMapper.EntityTypeFor(kind), id,
            before, LookupMapper.ToSnapshot(current));

        await _lookups.SaveChangesAsync(cancellationToken);

        LookupLog.Updated(_logger, kind.ToString(), id, current.Name, _currentUser.Name);

        return await SucceededAsync(kind, LookupMutationStatus.Updated, id, cancellationToken);
    }

    public async Task<LookupMutationResult> DeleteAsync(
        LookupKind kind, int id, CancellationToken cancellationToken = default)
    {
        var current = await _lookups.GetForUpdateAsync(kind, id, cancellationToken);
        if (current is null) return NotFound(kind, id);

        if (current.IsDeleted)
        {
            return Refused(kind, LookupMutationResult.Failure(
                LookupMutationStatus.AlreadyInThatState,
                $"{LookupMapper.DisplayName(kind)} '{current.Name}' is already deleted."), id);
        }

        // Deletion is refused while live data still points here. Restore stays
        // unconditional - a row that came back was fine before it left.
        var inUse = await _lookups.CountLiveReferencesAsync(kind, id, cancellationToken);
        if (inUse > 0)
        {
            return Refused(kind, LookupMutationResult.Failure(
                LookupMutationStatus.InUse,
                $"{LookupMapper.DisplayName(kind)} '{current.Name}' is still used by {inUse} " +
                $"{LookupMapper.ReferenceDescription(kind)}. Remove the references first."), id);
        }

        var before = LookupMapper.ToSnapshot(current);

        current.IsDeleted = true;
        current.DeletedAt = DateTime.UtcNow;
        current.DeletedBy = _currentUser.Name;

        _audit.Record(AuditAction.Deleted, LookupMapper.EntityTypeFor(kind), id,
            before, before with { IsDeleted = true });

        await _lookups.SaveChangesAsync(cancellationToken);

        LookupLog.Deleted(_logger, kind.ToString(), id, current.Name, _currentUser.Name);

        return await SucceededAsync(kind, LookupMutationStatus.Deleted, id, cancellationToken);
    }

    public async Task<LookupMutationResult> RestoreAsync(
        LookupKind kind, int id, CancellationToken cancellationToken = default)
    {
        var current = await _lookups.GetForUpdateAsync(kind, id, cancellationToken);
        if (current is null) return NotFound(kind, id);

        if (!current.IsDeleted)
        {
            return Refused(kind, LookupMutationResult.Failure(
                LookupMutationStatus.AlreadyInThatState,
                $"{LookupMapper.DisplayName(kind)} '{current.Name}' is not deleted."), id);
        }

        var before = LookupMapper.ToSnapshot(current);

        Undelete(current);

        _audit.Record(AuditAction.Updated, LookupMapper.EntityTypeFor(kind), id,
            before, before with { IsDeleted = false });

        await _lookups.SaveChangesAsync(cancellationToken);

        LookupLog.Restored(_logger, kind.ToString(), id, current.Name, _currentUser.Name);

        return await SucceededAsync(kind, LookupMutationStatus.Restored, id, cancellationToken);
    }

    private async Task<LookupMutationResult> ReviveAsync(
        LookupKind kind, ILookupEntity existing, LookupInput input,
        CancellationToken cancellationToken)
    {
        var before = LookupMapper.ToSnapshot(existing);

        LookupMapper.Apply(existing, input);
        Undelete(existing);

        _audit.Record(AuditAction.Updated, LookupMapper.EntityTypeFor(kind), existing.Id,
            before, LookupMapper.ToSnapshot(existing));

        await _lookups.SaveChangesAsync(cancellationToken);

        LookupLog.Revived(_logger, kind.ToString(), existing.Id, existing.Name, _currentUser.Name);

        return await SucceededAsync(
            kind, LookupMutationStatus.Restored, existing.Id, cancellationToken);
    }

    private static void Undelete(ILookupEntity entity)
    {
        entity.IsDeleted = false;
        entity.DeletedAt = null;
        entity.DeletedBy = null;
    }

    private async Task<LookupMutationResult> SucceededAsync(
        LookupKind kind, LookupMutationStatus status, int id, CancellationToken cancellationToken)
    {
        var item = await GetAsync(kind, id, cancellationToken);

        return LookupMutationResult.Success(status, item!);
    }

    private LookupMutationResult Refused(
        LookupKind kind, LookupMutationResult result, int id = 0)
    {
        LookupLog.WriteRefused(
            _logger, kind.ToString(), id, result.Status.ToString(), result.Error ?? string.Empty);

        return result;
    }

    private LookupMutationResult NotFound(LookupKind kind, int id) =>
        Refused(kind, LookupMutationResult.Failure(
            LookupMutationStatus.NotFound,
            $"No {LookupMapper.DisplayName(kind)} with id {id}."), id);
}
