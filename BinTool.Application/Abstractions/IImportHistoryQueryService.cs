using BinTool.Application.Models.BinRanges;
using BinTool.Application.Models.Import;

namespace BinTool.Application.Abstractions;

public interface IImportHistoryQueryService
{
    Task<PagedResult<ImportHistoryItem>> SearchAsync(
        ImportHistoryQuery query, CancellationToken cancellationToken = default);
}
