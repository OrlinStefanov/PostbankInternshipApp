using Microsoft.Extensions.Logging;

namespace BinTool.Application.Services;

// One set of events for all four lookup tables, with the table as a field. A search for "everything
// that happened to funding types" is then a filter on {Kind}, which four separate event sets would
// not allow.
internal static partial class LookupLog
{
    [LoggerMessage(EventId = 1601, Level = LogLevel.Information,
        Message = "{Kind} {LookupId} '{Name}' created by {User}.")]
    public static partial void Created(
        ILogger logger, string kind, int lookupId, string name, string user);

    [LoggerMessage(EventId = 1602, Level = LogLevel.Information,
        Message = "{Kind} {LookupId} '{Name}' updated by {User}.")]
    public static partial void Updated(
        ILogger logger, string kind, int lookupId, string name, string user);

    [LoggerMessage(EventId = 1603, Level = LogLevel.Information,
        Message = "{Kind} {LookupId} '{Name}' deleted by {User}.")]
    public static partial void Deleted(
        ILogger logger, string kind, int lookupId, string name, string user);

    [LoggerMessage(EventId = 1604, Level = LogLevel.Information,
        Message = "{Kind} {LookupId} '{Name}' restored by {User}.")]
    public static partial void Restored(
        ILogger logger, string kind, int lookupId, string name, string user);

    [LoggerMessage(EventId = 1605, Level = LogLevel.Information,
        Message = "{Kind} {LookupId} '{Name}' was re-added by {User}, reviving the deleted row.")]
    public static partial void Revived(
        ILogger logger, string kind, int lookupId, string name, string user);

    [LoggerMessage(EventId = 1701, Level = LogLevel.Warning,
        Message = "{Kind} write refused for {LookupId} as {Status}: {Reason}")]
    public static partial void WriteRefused(
        ILogger logger, string kind, int lookupId, string status, string reason);
}
