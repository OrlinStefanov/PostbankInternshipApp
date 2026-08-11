namespace BinTool.Api.Controllers;

/// <summary>
/// The log events sign-in emits.
/// <para>
/// <b>What is never logged here:</b> the password, the issued token, or any part of either.
/// The user name and the outcome are enough to investigate an account, and are the most that
/// may be written down. A refusal never says which of the two halves was wrong - the log
/// would then answer the question the 401 deliberately refuses to answer, namely whether an
/// account exists.
/// </para>
/// </summary>
internal static partial class AuthLog
{
    [LoggerMessage(EventId = 3001, Level = LogLevel.Information,
        Message = "{UserName} signed in; the token expires at {ExpiresAtUtc:u}.")]
    public static partial void SignedIn(this ILogger logger, string userName, DateTime expiresAtUtc);

    [LoggerMessage(EventId = 3002, Level = LogLevel.Warning,
        Message = "Sign-in refused for {UserName}: the credentials were rejected.")]
    public static partial void CredentialsRejected(this ILogger logger, string userName);

    [LoggerMessage(EventId = 3003, Level = LogLevel.Warning,
        Message = "Sign-in refused for {UserName}: the account is locked out until {LockoutEnd:u}. " +
                  "The correct password would unlock it now; otherwise it clears on its own then.")]
    public static partial void LockedOut(
        this ILogger logger, string? userName, DateTimeOffset? lockoutEnd);
}
