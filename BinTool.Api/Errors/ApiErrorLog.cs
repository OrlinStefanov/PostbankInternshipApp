namespace BinTool.Api.Errors;

internal static partial class ApiErrorLog
{
    [LoggerMessage(EventId = 3101, Level = LogLevel.Error,
        Message = "Unhandled exception answering {Method} {Path}.")]
    public static partial void Unhandled(
        ILogger logger, Exception exception, string method, string path);

    [LoggerMessage(EventId = 3102, Level = LogLevel.Warning,
        Message = "Refused {Method} {Path}: {Reason}")]
    public static partial void Refused(ILogger logger, string method, string path, string reason);
}
