using BinTool.Core.Models.Import;

namespace BinTool.Core.Services;

public interface IBinCsvImportService
{
    /// <summary>
    /// Parses and validates the CSV, then reconciles it against the database:
    /// new prefixes are inserted, rejected rows are persisted, unchanged rows are
    /// skipped, and prefixes that already exist with different values are staged
    /// as conflicts for the user to resolve later. All within one transaction.
    /// </summary>
    /// <param name="csvStream">The uploaded CSV content.</param>
    /// <param name="fileName">Original file name, recorded in the import history.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<BinImportResult> ImportAsync(Stream csvStream, string fileName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the conflicts still awaiting a decision (status Pending), each with a
    /// freshly-computed diff against the current database record. Used to restore the
    /// resolution workflow after a page reload, when no in-memory import result exists.
    /// </summary>
    Task<List<BinConflict>> GetPendingConflictsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Applies the user's decisions to staged conflicts: an <c>Update</c> overwrites
    /// the existing BIN range with the staged values; otherwise the conflict is
    /// discarded and the existing record is left untouched.
    /// </summary>
    Task<ConflictResolutionResult> ResolveConflictsAsync(
        IEnumerable<ConflictResolution> resolutions, CancellationToken cancellationToken = default);
}
