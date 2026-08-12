namespace BinTool.Application.Models.Auth;

public enum LoginRejectionReason
{
    /// <summary>
    /// The user name was unknown, the password was wrong, or the account is deactivated - all
    /// reported the same way so the response cannot be used to discover which accounts exist.
    /// </summary>
    InvalidCredentials,

    /// <summary>The account is locked after too many failed attempts.</summary>
    LockedOut
}

public class LoginRejection
{
    /// <summary>Why the sign-in was refused.</summary>
    public LoginRejectionReason Reason { get; set; }

    /// <summary>
    /// When the lockout ends, in UTC. Set only when <see cref="Reason"/> is <see
    /// cref="LoginRejectionReason.LockedOut"/>; null otherwise.
    /// </summary>
    public DateTime? LockoutEndsUtc { get; set; }
}
