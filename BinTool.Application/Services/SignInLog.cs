using Microsoft.Extensions.Logging;

namespace BinTool.Application.Services;

public static partial class SignInLog
{
    [LoggerMessage(EventId = 3001, Level = LogLevel.Information,
        Message = "{UserName} signed in; the token expires at {ExpiresAtUtc:u}.")]
    public static partial void SignedIn(ILogger logger, string userName, DateTime expiresAtUtc);

    [LoggerMessage(EventId = 3002, Level = LogLevel.Warning,
        Message = "Sign-in refused for {UserName}: the credentials were rejected.")]
    public static partial void CredentialsRejected(ILogger logger, string userName);

    [LoggerMessage(EventId = 3003, Level = LogLevel.Warning,
        Message = "Sign-in refused for {UserName}: the account is locked out until {LockoutEnd:u}. " +
                  "The correct password would unlock it now; otherwise it clears on its own then.")]
    public static partial void LockedOut(
        ILogger logger, string? userName, DateTimeOffset? lockoutEnd);
}
