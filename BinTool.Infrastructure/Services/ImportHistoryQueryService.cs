using BinTool.Core.Entities;
using BinTool.Core.Models.BinRanges;
using BinTool.Core.Models.Import;
using BinTool.Core.Services;
using BinTool.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BinTool.Infrastructure.Services;

public class ImportHistoryQueryService : IImportHistoryQueryService
{
    /// <summary>
    /// Shown when an import was recorded with no user signed in - the same marker the audit
    /// listing uses.
    /// </summary>
    private const string SystemUser = "system";

    private readonly AppDbContext _db;

    public ImportHistoryQueryService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<PagedResult<ImportHistoryItem>> SearchAsync(
        ImportHistoryQuery query, CancellationToken cancellationToken = default)
    {
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(
            query.PageSize <= 0 ? ImportHistoryQuery.DefaultPageSize : query.PageSize,
            1, ImportHistoryQuery.MaxPageSize);

        var rows = ApplyFilters(_db.ImportHistories.AsNoTracking(), query);

        var totalCount = await rows.CountAsync(cancellationToken);

        var items = await rows
            // Newest first is what an operator scanning for a recent import wants; the id
            // tiebreaks two imports recorded in the same tick so paging stays stable.
            .OrderByDescending(h => h.ImportedAt)
            .ThenByDescending(h => h.ImportHistoryId)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(h => new ImportHistoryItem
            {
                ImportHistoryId = h.ImportHistoryId,
                FileName = h.FileName,
                ImportedAt = h.ImportedAt,
                ImportedRows = h.ImportedRows,
                UpdatedRows = h.UpdatedRows,
                RejectedRows = h.RejectedRows,
                Status = h.Status,
                UserName = h.ImportedByUser != null ? h.ImportedByUser.UserName! : SystemUser
            })
            .ToListAsync(cancellationToken);

        return new PagedResult<ImportHistoryItem>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    private static IQueryable<ImportHistory> ApplyFilters(
        IQueryable<ImportHistory> rows, ImportHistoryQuery query)
    {
        if (query.From is { } from)
        {
            rows = rows.Where(h => h.ImportedAt >= from);
        }

        if (query.To is { } to)
        {
            // Half-open: two adjacent day queries never claim the same row twice.
            rows = rows.Where(h => h.ImportedAt < to);
        }

        var status = query.Status?.Trim();
        if (!string.IsNullOrEmpty(status))
        {
            var normalized = status.ToLowerInvariant();
            rows = rows.Where(h => h.Status.ToLower() == normalized);
        }

        return rows;
    }
}
