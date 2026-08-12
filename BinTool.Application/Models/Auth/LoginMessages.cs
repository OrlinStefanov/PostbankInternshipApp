namespace BinTool.Application.Models.Auth;

public static class LoginMessages
{
    public const string Rejected =
        "Invalid user name or password, or the account is temporarily locked after too " +
        "many failed attempts.";

    public static string LockedOut(int minutes)
    {
        var window = minutes <= 1 ? "a minute" : $"{minutes} minutes";
        return $"This account is locked after too many failed attempts. Try again in {window}, " +
               "or sign in with the correct password to unlock it now.";
    }
}
