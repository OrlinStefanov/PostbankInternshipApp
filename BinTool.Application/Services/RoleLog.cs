using Microsoft.Extensions.Logging;

namespace BinTool.Application.Services;

internal static partial class RoleLog
{
    [LoggerMessage(EventId = 1801, Level = LogLevel.Information,
        Message = "Role '{RoleName}' ({RoleId}) created by {User} with {PermissionCount} permissions.")]
    public static partial void Created(
        ILogger logger, string roleId, string roleName, int permissionCount, string user);

    [LoggerMessage(EventId = 1802, Level = LogLevel.Information,
        Message = "Role '{RoleName}' ({RoleId}) updated by {User}; it now grants {PermissionCount} permissions.")]
    public static partial void Updated(
        ILogger logger, string roleId, string roleName, int permissionCount, string user);

    [LoggerMessage(EventId = 1803, Level = LogLevel.Information,
        Message = "Role '{RoleName}' ({RoleId}) deleted by {User}.")]
    public static partial void Deleted(ILogger logger, string roleId, string roleName, string user);

    [LoggerMessage(EventId = 1901, Level = LogLevel.Warning,
        Message = "Role write refused for '{RoleName}' as {Status}: {Reason}")]
    public static partial void WriteRefused(
        ILogger logger, string roleName, string status, string reason);
}
