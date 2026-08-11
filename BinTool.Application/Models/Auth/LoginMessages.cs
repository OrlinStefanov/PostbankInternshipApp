namespace BinTool.Application.Models.Auth;

public static class LoginMessages
{
    /// <summary>
    /// The single message every rejected sign-in gets, wherever it is shown.
    /// <para>
    /// A wrong password, an unknown user, a deactivated account and a locked-out account
    /// all read the same, so the answer cannot be used to discover which accounts exist.
    /// Lockout is named as one of the possibilities because it is the only cause the
    /// person signing in can neither see nor fix by trying again - and naming it among
    /// several gives away nothing about which one applied.
    /// </para>
    /// <para>
    /// Shared by the API and the UI so the two cannot drift apart: the UI never sees the
    /// API's body on a rejection, so without this it would be keeping its own copy.
    /// </para>
    /// </summary>
    public const string Rejected =
        "Invalid user name or password, or the account is temporarily locked after too " +
        "many failed attempts.";

    /// <summary>
    /// Shown when a sign-in is refused specifically because the account is locked out.
    /// <para>
    /// Unlike <see cref="Rejected"/>, this names the cause and how long is left, because a
    /// lockout is the one refusal the person signing in can neither see nor fix by retrying -
    /// so leaving them to guess only makes them hammer a lock that only time (or their
    /// correct password) clears. Shared by the API and the UI so the two cannot drift apart.
    /// </para>
    /// </summary>
    public static string LockedOut(int minutes)
    {
        var window = minutes <= 1 ? "a minute" : $"{minutes} minutes";
        return $"This account is locked after too many failed attempts. Try again in {window}, " +
               "or sign in with the correct password to unlock it now.";
    }
}
