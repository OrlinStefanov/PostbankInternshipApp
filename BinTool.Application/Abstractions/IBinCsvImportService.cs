using BinTool.Application.Models.Import;

namespace BinTool.Application.Abstractions;

public interface IBinCsvImportService
{
    /// <summary>
    /// Parses and validates the CSV, then reconciles it against the database: new prefixes are
    /// inserted, rejected rows are persisted, unchanged rows are skipped, and prefixes that already
    /// exist with different values are staged as conflicts for the user to resolve later.
    /// </summary>
    Task<BinImportResult> ImportAsync(Stream csvStream, string fileName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the conflicts still awaiting a decision (status Pending), each with a
    /// freshly-computed diff against the current database record.
    /// </summary>
    Task<List<BinConflict>> GetPendingConflictsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Applies the user's decisions to staged conflicts: an <c>Update</c> overwrites the existing
    /// BIN range with the staged values; otherwise the conflict is discarded and the existing
    /// record is left untouched.
    /// </summary>
    Task<ConflictResolutionResult> ResolveConflictsAsync(
        IEnumerable<ConflictResolution> resolutions, CancellationToken cancellationToken = default);
}
