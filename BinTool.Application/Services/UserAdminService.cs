using BinTool.Application.Abstractions;
using BinTool.Application.Authorization;
using BinTool.Application.Mapping;
using BinTool.Application.Models.Access;
using BinTool.Application.Models.Audit;
using Microsoft.Extensions.Logging;

namespace BinTool.Application.Services;

public class UserAdminService : IUserAdminService
{
    private readonly IUserRepository _users;
    private readonly ICurrentUser _currentUser;
    private readonly IAuditLog _audit;
    private readonly ILogger<UserAdminService> _logger;

    public UserAdminService(
        IUserRepository users,
        ICurrentUser currentUser,
        IAuditLog audit,
        ILogger<UserAdminService> logger)
    {
        _users = users;
        _currentUser = currentUser;
        _audit = audit;
        _logger = logger;
    }

    public async Task<IReadOnlyList<UserListItem>> GetUsersAsync(
        CancellationToken cancellationToken = default)
    {
        var users = await _users.ListAsync(cancellationToken);

        return users.Select(UserMapper.ToListItem).ToList();
    }

    public async Task<UserRolesResult> SetRolesAsync(
        string userId, UserRolesInput input, CancellationToken cancellationToken = default)
    {
        var user = await _users.FindByIdAsync(userId, cancellationToken);
        if (user is null)
        {
            return Refused(UserRolesResult.Failure(
                UserRolesStatus.NotFound, "No user has that id."), userId);
        }

        var wanted = input.WantedRoles();

        foreach (var roleName in wanted)
        {
            if (!await _users.RoleExistsAsync(roleName, cancellationToken))
            {
                return Refused(UserRolesResult.Failure(
                    UserRolesStatus.UnknownRole, $"Role '{roleName}' does not exist."), user.UserName);
            }
        }

        var current = user.Roles.ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (current.Contains(AppRoles.Admin) && !wanted.Contains(AppRoles.Admin))
        {
            var refusal = await GuardLastAdminAsync(user, cancellationToken);
            if (refusal is not null) return refusal;
        }

        var toAdd = wanted.Except(current).ToList();
        var toRemove = current.Except(wanted).ToList();

        if (toAdd.Count == 0 && toRemove.Count == 0)
        {
            // Nothing changed: report success without writing an audit row for a no-op.
            return UserRolesResult.Success(UserMapper.ToListItem(user));
        }

        await _users.AddToRolesAsync(userId, toAdd, cancellationToken);
        await _users.RemoveFromRolesAsync(userId, toRemove, cancellationToken);

        var after = await _users.FindByIdAsync(userId, cancellationToken) ?? user;

        _audit.Record(AuditAction.Updated, AuditEntityTypes.UserRole, 0,
            UserMapper.ToRolesSnapshot(user, current.OrderBy(r => r).ToArray()),
            UserMapper.ToRolesSnapshot(after, after.Roles.ToArray()));

        await _users.SaveChangesAsync(cancellationToken);

        UserLog.RolesChanged(
            _logger, user.Id, user.UserName, string.Join(", ", toAdd),
            string.Join(", ", toRemove), _currentUser.Name);

        return UserRolesResult.Success(UserMapper.ToListItem(after));
    }

    // Losing Admin is the one role change that can make the system unadministrable, so it
    // is refused in the two cases where nobody would be left to undo it.
    private async Task<UserRolesResult?> GuardLastAdminAsync(
        UserRecord user, CancellationToken cancellationToken)
    {
        if (string.Equals(user.Id, _currentUser.UserId, StringComparison.Ordinal))
        {
            return Refused(UserRolesResult.Failure(
                UserRolesStatus.SelfDemotion,
                "You can't remove your own Admin role. Ask another admin to do it."), user.UserName);
        }

        var admins = await _users.CountUsersInRoleAsync(AppRoles.Admin, cancellationToken);
        if (admins <= 1)
        {
            return Refused(UserRolesResult.Failure(
                UserRolesStatus.LastAdmin,
                "This is the last admin. Grant Admin to another user before removing it here."),
                user.UserName);
        }

        return null;
    }

    private UserRolesResult Refused(UserRolesResult result, string subject)
    {
        UserLog.RoleChangeRefused(
            _logger, subject, result.Status.ToString(), result.Error ?? string.Empty);

        return result;
    }
}
