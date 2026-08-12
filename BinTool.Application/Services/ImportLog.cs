using Microsoft.Extensions.Logging;

namespace BinTool.Application.Services;

// Nothing here logs per row - one import can touch thousands, and the row-level detail belongs in
// BinImportResult. Rejection reasons are logged; the rejected line itself is not, being unvalidated
// caller input.
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
