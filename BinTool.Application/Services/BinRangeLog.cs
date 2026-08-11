using Microsoft.Extensions.Logging;

namespace BinTool.Application.Services;

// A BIN prefix is the first six to eight digits of a card number and identifies the issuer, not the
// cardholder - it is reference data and safe to log. A full card number never is, and nothing here
// handles one.
internal static partial class BinRangeLog
{
    [LoggerMessage(EventId = 2301, Level = LogLevel.Information,
        Message = "BIN range {BinRangeId} {Prefix} created by {User} as {CardScheme}.")]
    public static partial void Created(
        ILogger logger, int binRangeId, string prefix, string cardScheme, string user);

    [LoggerMessage(EventId = 2302, Level = LogLevel.Information,
        Message = "BIN range {BinRangeId} {Prefix} updated by {User}.")]
    public static partial void Updated(ILogger logger, int binRangeId, string prefix, string user);

    [LoggerMessage(EventId = 2303, Level = LogLevel.Information,
        Message = "BIN range {BinRangeId} {Prefix} deleted by {User}.")]
    public static partial void Deleted(ILogger logger, int binRangeId, string prefix, string user);

    [LoggerMessage(EventId = 2304, Level = LogLevel.Information,
        Message = "BIN range {BinRangeId} {Prefix} restored by {User}.")]
    public static partial void Restored(ILogger logger, int binRangeId, string prefix, string user);

    [LoggerMessage(EventId = 2305, Level = LogLevel.Information,
        Message = "BIN range {BinRangeId} {Prefix} was re-added by {User}, reviving the deleted row.")]
    public static partial void Revived(ILogger logger, int binRangeId, string prefix, string user);

    [LoggerMessage(EventId = 2401, Level = LogLevel.Warning,
        Message = "BIN range write refused for {Prefix}: the digits look like {DetectedScheme} " +
                  "but {DeclaredScheme} was declared, and {User} did not acknowledge it.")]
    public static partial void SchemeMismatchRefused(
        ILogger logger, string prefix, string declaredScheme, string detectedScheme, string user);

    [LoggerMessage(EventId = 2402, Level = LogLevel.Warning,
        Message = "BIN range write refused for {Prefix} as {Status}: {Reason}")]
    public static partial void WriteRefused(
        ILogger logger, string prefix, string status, string reason);
}
