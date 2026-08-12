using BinTool.Application.Models.BinRanges;
using BinTool.Application.Models.Import;

namespace BinTool.Application.Abstractions;

public interface IImportHistoryQueryService
{
    /// <summary>
    /// Returns one page of past imports matching the filters, newest first, with the user id
    /// resolved to a user name.
    /// </summary>
    Task<PagedResult<ImportHistoryItem>> SearchAsync(
        ImportHistoryQuery query, CancellationToken cancellationToken = default);
}
