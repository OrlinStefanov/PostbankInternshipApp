using BinTool.Application.Models.Audit;
using BinTool.Application.Models.BinRanges;
using BinTool.Infrastructure.Data;

namespace BinTool.Infrastructure.Repositories;

public class AuditRepository : IAuditRepository
{
    private readonly AppDbContext _db;

    public AuditRepository(AppDbContext db)
    {
        _db = db;
    }

    public void Add(AuditEntry entry) => _db.AuditEntries.Add(entry);

    public async Task<PagedResult<AuditLogItem>> SearchAsync(
        AuditQuery query, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var rows = ApplyFilters(_db.AuditEntries.AsNoTracking(), query);

        var totalCount = await rows.CountAsync(cancellationToken);

        var items = await rows
            .OrderByDescending(a => a.PerformedAt)
            .ThenByDescending(a => a.AuditEntryId)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(a => new AuditLogItem
            {
                AuditEntryId = a.AuditEntryId,
                PerformedAt = a.PerformedAt,
                Action = a.Action,
                EntityType = a.EntityType,
                EntityId = a.EntityId,
                UserName = a.PerformedByUser != null ? a.PerformedByUser.UserName! : ICurrentUser.SystemName,
                OldValues = a.OldValues,
                NewValues = a.NewValues
            })
            .ToListAsync(cancellationToken);

        return new PagedResult<AuditLogItem>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    private static IQueryable<AuditEntry> ApplyFilters(
        IQueryable<AuditEntry> rows, AuditQuery query)
    {
        if (query.From is { } from)
        {
            rows = rows.Where(a => a.PerformedAt >= from);
        }

        if (query.To is { } to)
        {
            // Half-open: two adjacent day queries never claim the same row twice.
            rows = rows.Where(a => a.PerformedAt < to);
        }

        var entityType = Normalize(query.EntityType);
        if (entityType is not null)
        {
            rows = rows.Where(a => a.EntityType.ToLower() == entityType);
        }

        var userName = Normalize(query.UserName);
        if (userName is not null)
        {
            // "system" is the marker recorded when nobody was signed in, not a real user,
            // so filtering to it returns rows with a null user reference.
            if (userName == ICurrentUser.SystemName)
            {
                rows = rows.Where(a => a.PerformedByUserId == null);
            }
            else
            {
                rows = rows.Where(a => a.PerformedByUser != null
                    && a.PerformedByUser.UserName!.ToLower().StartsWith(userName));
            }
        }

        if (query.Action is { } action)
        {
            rows = rows.Where(a => a.Action == action);
        }

        return rows;
    }

    private static string? Normalize(string? value)
    {
        var trimmed = value?.Trim();
        return string.IsNullOrEmpty(trimmed) ? null : trimmed.ToLowerInvariant();
    }
}
