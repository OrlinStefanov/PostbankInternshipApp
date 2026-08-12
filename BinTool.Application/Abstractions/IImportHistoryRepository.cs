using BinTool.Application.Models.BinRanges;
using BinTool.Application.Models.Import;

namespace BinTool.Application.Abstractions;

public interface IImportHistoryRepository
{
    Task<PagedResult<ImportHistoryItem>> SearchAsync(
        ImportHistoryQuery query, int page, int pageSize,
        CancellationToken cancellationToken = default);
}
