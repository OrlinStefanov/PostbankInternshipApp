using Microsoft.Extensions.Logging;

namespace BinTool.Application.Services;

/// <summary>
/// The log events a BIN import emits.
/// <para>
/// A single import can touch thousands of rows, so nothing here logs per row - the row-level
/// detail belongs in <c>BinImportResult</c> and the rejection rows, which the user can read
/// in the UI. What the log carries is the shape of the run: what came in, what happened, and
/// what needs a human. The per-run scope ties those together.
/// </para>
/// <para>
/// Rejection reasons are logged; the rejected line itself is not. An import file is card
/// reference data rather than card numbers, but a raw CSV line is unvalidated caller input
/// and a log is the wrong place to find out it held something it should not have.
/// </para>
/// </summary>
internal static partial class ImportLog
{
    [LoggerMessage(EventId = 2001, Level = LogLevel.Information,
        Message = "Import of {FileName} started by {User}.")]
    public static partial void Started(ILogger logger, string fileName, string user);

    [LoggerMessage(EventId = 2002, Level = LogLevel.Information,
        Message = "Import of {FileName} finished as {Status}: {TotalRows} rows read, " +
                  "{InsertedRows} inserted, {UnchangedRows} unchanged, {RejectedRows} rejected, " +
                  "{ConflictCount} awaiting a decision.")]
    public static partial void Finished(
        ILogger logger, string fileName, string status, int totalRows, int insertedRows,
        int unchangedRows, int rejectedRows, int conflictCount);

    [LoggerMessage(EventId = 2003, Level = LogLevel.Warning,
        Message = "Import of {FileName} was rejected outright: {Reason}")]
    public static partial void FileRejected(ILogger logger, string fileName, string reason);

    [LoggerMessage(EventId = 2004, Level = LogLevel.Warning,
        Message = "Import of {FileName} left {ConflictCount} conflicts pending; " +
                  "the imported data is incomplete until someone resolves them.")]
    public static partial void ConflictsPending(
        ILogger logger, string fileName, int conflictCount);

    [LoggerMessage(EventId = 2005, Level = LogLevel.Information,
        Message = "{ResolvedCount} import conflicts resolved by {User}: " +
                  "{UpdatedCount} applied, {DiscardedCount} discarded.")]
    public static partial void ConflictsResolved(
        ILogger logger, int resolvedCount, string user, int updatedCount, int discardedCount);
}
