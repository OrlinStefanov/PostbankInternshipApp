using BinTool.Core.Entities;
using BinTool.Core.Models.Audit;
using BinTool.Core.Models.BinRanges;
using BinTool.Core.Services;
using BinTool.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BinTool.Infrastructure.Services;

public class AuditQueryService : IAuditQueryService
{
    /// <summary>
    /// Recorded on rows written with no user signed in - a background job, or a change
    /// that reached the audit path before authentication existed.
    /// </summary>
    private const string SystemUser = "system";

    private static readonly IReadOnlyList<string> EntityTypes = new[]
    {
        AuditEntityTypes.BinRange,
        AuditEntityTypes.CardScheme,
        AuditEntityTypes.ProductType,
        AuditEntityTypes.FundingType,
        AuditEntityTypes.Region,
        AuditEntityTypes.Country,
        AuditEntityTypes.CommissionRule,
        AuditEntityTypes.DefaultRule,
        AuditEntityTypes.Role,
        AuditEntityTypes.UserRole
    };

    private readonly AppDbContext _db;

    public AuditQueryService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<PagedResult<AuditLogItem>> SearchAsync(
        AuditQuery query, CancellationToken cancellationToken = default)
    {
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(
            query.PageSize <= 0 ? AuditQuery.DefaultPageSize : query.PageSize,
            1, AuditQuery.MaxPageSize);

        var rows = ApplyFilters(_db.AuditEntries.AsNoTracking(), query);

        var totalCount = await rows.CountAsync(cancellationToken);

        var items = await rows
            // Newest first is what an operator scanning for a recent change wants; the id
            // tiebreaks two rows written in the same tick so paging stays stable.
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
                UserName = a.PerformedByUser != null ? a.PerformedByUser.UserName! : SystemUser,
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

    public IReadOnlyList<string> GetEntityTypes() => EntityTypes;

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
            if (userName == SystemUser)
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
