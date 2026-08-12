using BinTool.Application.Models.Auth;
using Microsoft.AspNetCore.Identity;

namespace BinTool.Infrastructure.Repositories;

// Identity-backed credentials. Every method is one UserManager call against a user found by id.
public class CredentialStore : ICredentialStore
{
    private readonly UserManager<ApplicationUser> _users;

    public CredentialStore(UserManager<ApplicationUser> users)
    {
        _users = users;
    }

    public async Task<CredentialUser?> FindByNameOrEmailAsync(
        string userName, CancellationToken cancellationToken = default)
    {
        var user = await _users.FindByNameAsync(userName)
                   ?? await _users.FindByEmailAsync(userName);

        return user is null
            ? null
            : new CredentialUser(
                user.Id, user.UserName ?? string.Empty, user.Email, user.FullName, user.IsActive);
    }

    public async Task<bool> CheckPasswordAsync(
        string userId, string password, CancellationToken cancellationToken = default)
    {
        var user = await _users.FindByIdAsync(userId);

        return user is not null && await _users.CheckPasswordAsync(user, password);
    }

    public async Task<DateTimeOffset?> GetLockoutEndAsync(
        string userId, CancellationToken cancellationToken = default)
    {
        var user = await _users.FindByIdAsync(userId);

        return user is null ? null : await _users.GetLockoutEndDateAsync(user);
    }

    public async Task ClearLockoutAsync(string userId, CancellationToken cancellationToken = default)
    {
        var user = await _users.FindByIdAsync(userId);
        if (user is null) return;

        if (await _users.GetLockoutEndDateAsync(user) is not null)
        {
            await _users.SetLockoutEndDateAsync(user, null);
        }

        await _users.ResetAccessFailedCountAsync(user);
    }

    public async Task<bool> RecordFailedAttemptAsync(
        string userId, CancellationToken cancellationToken = default)
    {
        var user = await _users.FindByIdAsync(userId);
        if (user is null) return false;

        await _users.AccessFailedAsync(user);

        return await _users.IsLockedOutAsync(user);
    }

    public async Task<IReadOnlyList<string>> GetRolesAsync(
        string userId, CancellationToken cancellationToken = default)
    {
        var user = await _users.FindByIdAsync(userId);

        return user is null ? Array.Empty<string>() : (IReadOnlyList<string>)await _users.GetRolesAsync(user);
    }

    public async Task StampLastLoginAsync(
        string userId, DateTime whenUtc, CancellationToken cancellationToken = default)
    {
        var user = await _users.FindByIdAsync(userId);
        if (user is null) return;

        user.LastLoginAt = whenUtc;

        await _users.UpdateAsync(user);
    }
}
