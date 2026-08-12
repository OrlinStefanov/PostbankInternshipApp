using BinTool.Application.Models.Import;

namespace BinTool.Application.Abstractions;

public interface IBinCsvImportService
{
    Task<BinImportResult> ImportAsync(Stream csvStream, string fileName, CancellationToken cancellationToken = default);

    Task<List<BinConflict>> GetPendingConflictsAsync(CancellationToken cancellationToken = default);

    Task<ConflictResolutionResult> ResolveConflictsAsync(
        IEnumerable<ConflictResolution> resolutions, CancellationToken cancellationToken = default);
}
