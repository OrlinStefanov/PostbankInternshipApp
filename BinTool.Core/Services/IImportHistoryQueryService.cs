using BinTool.Core.Models.BinRanges;
using BinTool.Core.Models.Import;

namespace BinTool.Core.Services;

public interface IImportHistoryQueryService
{
    /// <summary>
    /// Returns one page of past imports matching the filters, newest first, with the
    /// user id resolved to a user name.
    /// </summary>
    Task<PagedResult<ImportHistoryItem>> SearchAsync(
        ImportHistoryQuery query, CancellationToken cancellationToken = default);
}
