using BinTool.Application.Models.BinRanges;
using BinTool.Application.Models.Import;

namespace BinTool.Application.Abstractions;

/// <summary>
/// Read access to the record of past imports. Writing history is the import's own job and
/// belongs to <see cref="IBinImportRepository"/>, which owns the same unit of work the rows
/// are saved in.
/// </summary>
public interface IImportHistoryRepository
{
    Task<PagedResult<ImportHistoryItem>> SearchAsync(
        ImportHistoryQuery query, int page, int pageSize,
        CancellationToken cancellationToken = default);
}
