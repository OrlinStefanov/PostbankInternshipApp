using Microsoft.Extensions.Logging;

namespace BinTool.Application.Services;

internal static partial class UserLog
{
    [LoggerMessage(EventId = 2101, Level = LogLevel.Information,
        Message = "Roles for {UserName} ({UserId}) changed by {ChangedBy}: granted [{Granted}], revoked [{Revoked}].")]
    public static partial void RolesChanged(
        ILogger logger, string userId, string userName,
        string granted, string revoked, string changedBy);

    [LoggerMessage(EventId = 2201, Level = LogLevel.Warning,
        Message = "Role change refused for {UserName} as {Status}: {Reason}")]
    public static partial void RoleChangeRefused(
        ILogger logger, string userName, string status, string reason);
}
