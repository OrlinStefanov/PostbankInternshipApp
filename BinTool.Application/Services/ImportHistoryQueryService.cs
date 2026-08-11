using BinTool.Application.Abstractions;
using BinTool.Application.Models.BinRanges;
using BinTool.Application.Models.Import;

namespace BinTool.Application.Services;

public class ImportHistoryQueryService : IImportHistoryQueryService
{
    private readonly IImportHistoryRepository _history;

    public ImportHistoryQueryService(IImportHistoryRepository history)
    {
        _history = history;
    }

    public Task<PagedResult<ImportHistoryItem>> SearchAsync(
        ImportHistoryQuery query, CancellationToken cancellationToken = default)
    {
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(
            query.PageSize <= 0 ? ImportHistoryQuery.DefaultPageSize : query.PageSize,
            1, ImportHistoryQuery.MaxPageSize);

        return _history.SearchAsync(query, page, pageSize, cancellationToken);
    }
}
