using BinTool.Application.Models.Auth;

namespace BinTool.Application.Mapping;

public static class SignInMapper
{
    public static LoginResponse ToResponse(
        AuthenticatedUser user, string token, DateTime expiresAtUtc) => new()
        {
            AccessToken = token,
            ExpiresAtUtc = expiresAtUtc,
            UserId = user.Id,
            UserName = user.UserName,
            FullName = user.FullName ?? string.Empty,
            Roles = user.Roles.ToList(),
            Permissions = user.Permissions.ToList()
        };

    public static LoginRejection ToRejection(SignInOutcome outcome) => new()
    {
        Reason = outcome.Reason ?? LoginRejectionReason.InvalidCredentials,
        LockoutEndsUtc = outcome.LockoutEndsUtc
    };
}
