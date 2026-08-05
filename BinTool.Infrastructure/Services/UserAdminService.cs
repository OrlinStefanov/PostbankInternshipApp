using BinTool.Core.Entities;
using BinTool.Core.Models.Access;
using BinTool.Core.Services;
using BinTool.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BinTool.Infrastructure.Services;

/// <summary>
/// Lists users and sets their roles. Guards the god account: the last admin keeps Admin, and
/// no one strips their own Admin role.
/// </summary>
public class UserAdminService : IUserAdminService
{
    private readonly UserManager<ApplicationUser> _users;
    private readonly RoleManager<ApplicationRole> _roles;
    private readonly ICurrentUser _currentUser;
    private readonly IAuditLog _audit;
    private readonly AppDbContext _db;

    public UserAdminService(
        UserManager<ApplicationUser> users,
        RoleManager<ApplicationRole> roles,
        ICurrentUser currentUser,
        IAuditLog audit,
        AppDbContext db)
    {
        _users = users;
        _roles = roles;
        _currentUser = currentUser;
        _audit = audit;
        _db = db;
    }

    public async Task<IReadOnlyList<UserListItem>> GetUsersAsync(
        CancellationToken cancellationToken = default)
    {
        var users = await _users.Users
            .OrderBy(u => u.UserName)
            .ToListAsync(cancellationToken);

        var items = new List<UserListItem>(users.Count);
        foreach (var user in users)
        {
            items.Add(await ToListItemAsync(user));
        }

        return items;
    }

    public async Task<UserRolesResult> SetRolesAsync(
        string userId, UserRolesInput input, CancellationToken cancellationToken = default)
    {
        var user = await _users.FindByIdAsync(userId);
        if (user is null)
        {
            return UserRolesResult.Failure(UserRolesStatus.NotFound, "No user has that id.");
        }

        var wanted = new HashSet<string>(
            input.Roles.Select(r => r.Trim()).Where(r => r.Length > 0),
            StringComparer.OrdinalIgnoreCase);

        foreach (var roleName in wanted)
        {
            if (!await _roles.RoleExistsAsync(roleName))
            {
                return UserRolesResult.Failure(
                    UserRolesStatus.UnknownRole, $"Role '{roleName}' does not exist.");
            }
        }

        var current = new HashSet<string>(await _users.GetRolesAsync(user), StringComparer.OrdinalIgnoreCase);

        var losingAdmin = current.Contains(AppRoles.Admin) && !wanted.Contains(AppRoles.Admin);
        if (losingAdmin)
        {
            if (string.Equals(user.Id, _currentUser.UserId, StringComparison.Ordinal))
            {
                return UserRolesResult.Failure(
                    UserRolesStatus.SelfDemotion,
                    "You can't remove your own Admin role. Ask another admin to do it.");
            }

            var admins = await _users.GetUsersInRoleAsync(AppRoles.Admin);
            if (admins.Count <= 1)
            {
                return UserRolesResult.Failure(
                    UserRolesStatus.LastAdmin,
                    "This is the last admin. Grant Admin to another user before removing it here.");
            }
        }

        var toAdd = wanted.Except(current).ToList();
        var toRemove = current.Except(wanted).ToList();

        if (toAdd.Count == 0 && toRemove.Count == 0)
        {
            // Nothing changed - report success without writing an audit row for a no-op.
            return UserRolesResult.Success(await ToListItemAsync(user));
        }

        if (toAdd.Count > 0)
        {
            await _users.AddToRolesAsync(user, toAdd);
        }

        if (toRemove.Count > 0)
        {
            await _users.RemoveFromRolesAsync(user, toRemove);
        }

        var after = await ToListItemAsync(user);

        _audit.Record(
            AuditAction.Updated, AuditEntityTypes.UserRole, 0,
            new { UserId = user.Id, UserName = user.UserName, Roles = current.OrderBy(r => r).ToArray() },
            new { UserId = user.Id, UserName = user.UserName, Roles = after.Roles });
        await _db.SaveChangesAsync(cancellationToken);

        return UserRolesResult.Success(after);
    }

    private async Task<UserListItem> ToListItemAsync(ApplicationUser user) => new()
    {
        Id = user.Id,
        UserName = user.UserName ?? string.Empty,
        FullName = user.FullName,
        Email = user.Email,
        IsActive = user.IsActive,
        Roles = (await _users.GetRolesAsync(user)).OrderBy(r => r, StringComparer.OrdinalIgnoreCase).ToList()
    };
}
