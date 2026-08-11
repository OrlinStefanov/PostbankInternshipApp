namespace BinTool.Application.Models.Auth;

/// <summary>
/// Why a sign-in was refused.
/// </summary>
public enum LoginRejectionReason
{
    /// <summary>
    /// The user name was unknown, the password was wrong, or the account is deactivated -
    /// all reported the same way so the response cannot be used to discover which accounts
    /// exist.
    /// </summary>
    InvalidCredentials,

    /// <summary>
    /// The account is locked after too many failed attempts. The correct password would be
    /// refused too until the lock clears, so this is called out on its own -
    /// <see cref="LoginRejection.LockoutEndsUtc"/> says when it ends.
    /// </summary>
    LockedOut
}

/// <summary>
/// The body returned with a 401 from <c>POST /api/Auth/login</c>. Carries just enough to
/// tell a locked-out caller apart from a wrong password, without saying which account it was.
/// </summary>
public class LoginRejection
{
    /// <summary>
    /// Why the sign-in was refused.
    /// </summary>
    public LoginRejectionReason Reason { get; set; }

    /// <summary>
    /// When the lockout ends, in UTC. Set only when <see cref="Reason"/> is
    /// <see cref="LoginRejectionReason.LockedOut"/>; null otherwise.
    /// </summary>
    public DateTime? LockoutEndsUtc { get; set; }
}
