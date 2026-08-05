using System.Security.Claims;
using BinTool.Core.Authorization;
using BinTool.Core.Entities;
using BinTool.Core.Models.Access;
using BinTool.Core.Services;
using BinTool.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BinTool.Infrastructure.Services;

/// <summary>
/// Roles and their permissions, backed by Identity's role store. Permissions live as role
/// claims (type <see cref="PermissionClaimTypes.Permission"/>) so no extra table is needed, and
/// a login flattens them onto the user's token.
/// </summary>
public class RoleAdminService : IRoleAdminService
{
    private readonly RoleManager<ApplicationRole> _roles;
    private readonly UserManager<ApplicationUser> _users;
    private readonly ICurrentUser _currentUser;
    private readonly IAuditLog _audit;
    private readonly AppDbContext _db;

    public RoleAdminService(
        RoleManager<ApplicationRole> roles,
        UserManager<ApplicationUser> users,
        ICurrentUser currentUser,
        IAuditLog audit,
        AppDbContext db)
    {
        _roles = roles;
        _users = users;
        _currentUser = currentUser;
        _audit = audit;
        _db = db;
    }

    public IReadOnlyList<PermissionInfo> GetPermissionCatalog() => Permissions.All;

    public async Task<IReadOnlyList<RoleListItem>> GetRolesAsync(
        CancellationToken cancellationToken = default)
    {
        var roles = await _roles.Roles.ToListAsync(cancellationToken);

        var items = new List<RoleListItem>(roles.Count);
        foreach (var role in roles)
        {
            items.Add(await ToListItemAsync(role));
        }

        // Protected role first, then alphabetical - the god role belongs at the top.
        return items
            .OrderByDescending(r => r.IsProtected)
            .ThenBy(r => r.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public async Task<RoleMutationResult> CreateAsync(
        RoleInput input, CancellationToken cancellationToken = default)
    {
        var name = input.Name.Trim();

        if (await _roles.FindByNameAsync(name) is not null)
        {
            return RoleMutationResult.Failure(
                RoleMutationStatus.NameInUse, $"A role named '{name}' already exists.");
        }

        var role = new ApplicationRole(name) { Description = Clean(input.Description) };

        var created = await _roles.CreateAsync(role);
        if (!created.Succeeded)
        {
            return RoleMutationResult.Failure(RoleMutationStatus.Invalid, Describe(created));
        }

        await SetPermissionsAsync(role, Wanted(input));

        _audit.Record(AuditAction.Created, AuditEntityTypes.Role, 0, null, await SnapshotAsync(role));
        await _db.SaveChangesAsync(cancellationToken);

        return RoleMutationResult.Success(RoleMutationStatus.Created, await ToListItemAsync(role));
    }

    public async Task<RoleMutationResult> UpdateAsync(
        string roleId, RoleInput input, CancellationToken cancellationToken = default)
    {
        var role = await _roles.FindByIdAsync(roleId);
        if (role is null)
        {
            return RoleMutationResult.Failure(RoleMutationStatus.NotFound, "No role has that id.");
        }

        if (IsProtected(role))
        {
            return RoleMutationResult.Failure(
                RoleMutationStatus.Protected,
                "The Admin role is protected: it can't be renamed, deleted or have its permissions changed.");
        }

        var before = await SnapshotAsync(role);
        var name = input.Name.Trim();

        // Renaming onto a name another role owns would collide; renaming to its own name is fine.
        var owner = await _roles.FindByNameAsync(name);
        if (owner is not null && owner.Id != role.Id)
        {
            return RoleMutationResult.Failure(
                RoleMutationStatus.NameInUse, $"A role named '{name}' already exists.");
        }

        role.Name = name;
        role.Description = Clean(input.Description);

        var updated = await _roles.UpdateAsync(role);
        if (!updated.Succeeded)
        {
            return RoleMutationResult.Failure(RoleMutationStatus.Invalid, Describe(updated));
        }

        await SetPermissionsAsync(role, Wanted(input));

        _audit.Record(AuditAction.Updated, AuditEntityTypes.Role, 0, before, await SnapshotAsync(role));
        await _db.SaveChangesAsync(cancellationToken);

        return RoleMutationResult.Success(RoleMutationStatus.Updated, await ToListItemAsync(role));
    }

    public async Task<RoleMutationResult> DeleteAsync(
        string roleId, CancellationToken cancellationToken = default)
    {
        var role = await _roles.FindByIdAsync(roleId);
        if (role is null)
        {
            return RoleMutationResult.Failure(RoleMutationStatus.NotFound, "No role has that id.");
        }

        if (IsProtected(role))
        {
            return RoleMutationResult.Failure(
                RoleMutationStatus.Protected, "The Admin role is protected and can't be deleted.");
        }

        var memberCount = await MemberCountAsync(role);
        if (memberCount > 0)
        {
            return RoleMutationResult.Failure(
                RoleMutationStatus.InUse,
                $"This role is held by {memberCount} {(memberCount == 1 ? "user" : "users")}. " +
                "Remove it from them before deleting it.");
        }

        var before = await SnapshotAsync(role);

        var deleted = await _roles.DeleteAsync(role);
        if (!deleted.Succeeded)
        {
            return RoleMutationResult.Failure(RoleMutationStatus.Invalid, Describe(deleted));
        }

        _audit.Record(AuditAction.Deleted, AuditEntityTypes.Role, 0, before, null);
        await _db.SaveChangesAsync(cancellationToken);

        return RoleMutationResult.Success(RoleMutationStatus.Deleted);
    }

    /// <summary>
    /// Brings the role's permission claims in line with the wanted set: adds the missing ones,
    /// removes the ones no longer wanted, leaves the rest untouched.
    /// </summary>
    private async Task SetPermissionsAsync(ApplicationRole role, IReadOnlySet<string> wanted)
    {
        var current = await CurrentPermissionsAsync(role);

        foreach (var permission in wanted.Except(current))
        {
            await _roles.AddClaimAsync(role, new Claim(PermissionClaimTypes.Permission, permission));
        }

        foreach (var permission in current.Except(wanted))
        {
            await _roles.RemoveClaimAsync(role, new Claim(PermissionClaimTypes.Permission, permission));
        }
    }

    private async Task<HashSet<string>> CurrentPermissionsAsync(ApplicationRole role)
    {
        var claims = await _roles.GetClaimsAsync(role);

        return claims
            .Where(c => c.Type == PermissionClaimTypes.Permission)
            .Select(c => c.Value)
            .ToHashSet(StringComparer.Ordinal);
    }

    private async Task<RoleListItem> ToListItemAsync(ApplicationRole role) => new()
    {
        Id = role.Id,
        Name = role.Name ?? string.Empty,
        Description = role.Description,
        IsProtected = IsProtected(role),
        MemberCount = await MemberCountAsync(role),
        Permissions = (await CurrentPermissionsAsync(role))
            .OrderBy(p => p, StringComparer.Ordinal).ToList()
    };

    private async Task<int> MemberCountAsync(ApplicationRole role) =>
        role.Name is null ? 0 : (await _users.GetUsersInRoleAsync(role.Name)).Count;

    /// <summary>What the audit trail records for a role: names and keys a person can read.</summary>
    private async Task<object> SnapshotAsync(ApplicationRole role) => new
    {
        RoleId = role.Id,
        Name = role.Name,
        Description = role.Description,
        Permissions = (await CurrentPermissionsAsync(role))
            .OrderBy(p => p, StringComparer.Ordinal).ToArray()
    };

    /// <summary>Only known permission keys are kept - an unknown one is silently ignored here
    /// because the input model has already rejected it before a real request reaches this far.</summary>
    private static HashSet<string> Wanted(RoleInput input) =>
        input.Permissions.Where(Permissions.IsPermission).ToHashSet(StringComparer.Ordinal);

    private static bool IsProtected(ApplicationRole role) =>
        string.Equals(role.Name, AppRoles.Admin, StringComparison.OrdinalIgnoreCase);

    private static string? Clean(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string Describe(IdentityResult result) =>
        string.Join(" ", result.Errors.Select(e => e.Description));
}
