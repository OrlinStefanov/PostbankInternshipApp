namespace BinTool.Application.Models.Auth;

public static class LoginMessages
{
    /// <summary>The single message every rejected sign-in gets, wherever it is shown.</summary>
    public const string Rejected =
        "Invalid user name or password, or the account is temporarily locked after too " +
        "many failed attempts.";

    /// <summary>
    /// Shown when a sign-in is refused specifically because the account is locked out.
    /// </summary>
    public static string LockedOut(int minutes)
    {
        var window = minutes <= 1 ? "a minute" : $"{minutes} minutes";
        return $"This account is locked after too many failed attempts. Try again in {window}, " +
               "or sign in with the correct password to unlock it now.";
    }
}
