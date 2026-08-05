namespace BinTool.Core.Models.Auth;

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
}
