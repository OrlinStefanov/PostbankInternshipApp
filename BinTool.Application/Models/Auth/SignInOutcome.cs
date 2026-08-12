namespace BinTool.Application.Models.Auth;

public sealed record SignInOutcome(
    AuthenticatedUser? User,
    LoginRejectionReason? Reason,
    DateTime? LockoutEndsUtc)
{
    public static SignInOutcome Accepted(AuthenticatedUser user) => new(user, null, null);

    public static SignInOutcome Invalid() => new(null, LoginRejectionReason.InvalidCredentials, null);

    public static SignInOutcome Locked(DateTime? until) =>
        new(null, LoginRejectionReason.LockedOut, until);
}
