using BinTool.Application.Abstractions;
using BinTool.Application.Authorization;
using BinTool.Application.Models.Auth;
using Microsoft.Extensions.Logging;

namespace BinTool.Application.Services;

// The order of the checks is the rule worth holding on to: the password is checked before the lock,
// so an account's owner is never shut out by their own typos
public class SignInService : ISignInService
{
    private readonly ICredentialStore _credentials;
    private readonly IRoleRepository _roles;
    private readonly ILogger<SignInService> _logger;

    public SignInService(
        ICredentialStore credentials,
        IRoleRepository roles,
        ILogger<SignInService> logger)
    {
        _credentials = credentials;
        _roles = roles;
        _logger = logger;
    }

    public async Task<SignInOutcome> SignInAsync(
        string userName, string password, CancellationToken cancellationToken = default)
    {
        var user = await _credentials.FindByNameOrEmailAsync(userName, cancellationToken);

        if (user is null || !user.IsActive)
        {
            SignInLog.CredentialsRejected(_logger, userName);

            return SignInOutcome.Invalid();
        }

        if (!await _credentials.CheckPasswordAsync(user.Id, password, cancellationToken))
        {
            return await RefuseAsync(user, userName, cancellationToken);
        }

        await _credentials.ClearLockoutAsync(user.Id, cancellationToken);

        var roles = await _credentials.GetRolesAsync(user.Id, cancellationToken);
        var permissions = await ResolvePermissionsAsync(roles, cancellationToken);

        await _credentials.StampLastLoginAsync(user.Id, DateTime.UtcNow, cancellationToken);

        return SignInOutcome.Accepted(new AuthenticatedUser(
            user.Id, user.UserName, user.Email, user.FullName, roles, permissions));
    }

    private async Task<SignInOutcome> RefuseAsync(
        CredentialUser user, string userName, CancellationToken cancellationToken)
    {
        if (await _credentials.RecordFailedAttemptAsync(user.Id, cancellationToken))
        {
            var lockoutEnd = await _credentials.GetLockoutEndAsync(user.Id, cancellationToken);

            SignInLog.LockedOut(_logger, user.UserName, lockoutEnd);

            return SignInOutcome.Locked(lockoutEnd?.UtcDateTime);
        }

        SignInLog.CredentialsRejected(_logger, userName);

        return SignInOutcome.Invalid();
    }

    // Admin holds every key, including ones added to the catalog after it was seeded; every other
    // role holds what its claims say.
    private async Task<IReadOnlyList<string>> ResolvePermissionsAsync(
        IReadOnlyList<string> roleNames, CancellationToken cancellationToken)
    {
        var permissions = new HashSet<string>(StringComparer.Ordinal);

        if (roleNames.Any(r => string.Equals(r, AppRoles.Admin, StringComparison.Ordinal)))
        {
            permissions.UnionWith(Permissions.AllKeys);
        }

        var granted = roleNames
            .Where(r => !string.Equals(r, AppRoles.Admin, StringComparison.Ordinal))
            .ToList();

        if (granted.Count > 0)
        {
            permissions.UnionWith(await _roles.GetPermissionsAsync(granted, cancellationToken));
        }

        return permissions.ToList();
    }
}
