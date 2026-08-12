using BinTool.Application.Models.BinRanges;
using BinTool.Application.Models.Import;
using BinTool.Infrastructure.Data;

namespace BinTool.Infrastructure.Repositories;

public class ImportHistoryRepository : IImportHistoryRepository
{
    private readonly AppDbContext _db;

    public ImportHistoryRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task<PagedResult<ImportHistoryItem>> SearchAsync(
        ImportHistoryQuery query, int page, int pageSize,
        CancellationToken cancellationToken = default)
    {
        var rows = ApplyFilters(_db.ImportHistories.AsNoTracking(), query);

        var totalCount = await rows.CountAsync(cancellationToken);

        var items = await rows
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
                UserName = h.ImportedByUser != null ? h.ImportedByUser.UserName! : ICurrentUser.SystemName
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
