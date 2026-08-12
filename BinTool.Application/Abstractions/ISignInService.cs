using BinTool.Application.Models.Auth;

namespace BinTool.Application.Abstractions;

public interface ISignInService
{
    /// <summary>Checks credentials and, when they hold up, resolves what the account may do.</summary>
    Task<SignInOutcome> SignInAsync(
        string userName, string password, CancellationToken cancellationToken = default);
}

// A refusal carries the same reason the endpoint reports, so nothing has to be mapped on the way
// out. User is null exactly when the sign-in was refused.
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

public sealed record AuthenticatedUser(
    string Id,
    string UserName,
    string? Email,
    string? FullName,
    IReadOnlyList<string> Roles,
    IReadOnlyList<string> Permissions);
