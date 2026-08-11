using System.Security.Claims;
using BinTool.Application.Authorization;
using BinTool.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;

namespace BinTool.Infrastructure.Repositories;

/// <summary>
/// Identity-backed storage for roles. Permissions are held as role claims of type
/// <see cref="PermissionClaimTypes.Permission"/>, so no extra table is needed and a login can
/// flatten them onto the user's token.
/// </summary>
public class RoleRepository : IRoleRepository
{
    private readonly RoleManager<ApplicationRole> _roles;
    private readonly UserManager<ApplicationUser> _users;
    private readonly AppDbContext _db;

    public RoleRepository(
        RoleManager<ApplicationRole> roles,
        UserManager<ApplicationUser> users,
        AppDbContext db)
    {
        _roles = roles;
        _users = users;
        _db = db;
    }

    public async Task<IReadOnlyList<RoleRecord>> ListAsync(CancellationToken cancellationToken = default)
    {
        var roles = await _roles.Roles.ToListAsync(cancellationToken);

        var records = new List<RoleRecord>(roles.Count);
        foreach (var role in roles)
        {
            records.Add(await ToRecordAsync(role));
        }

        return records;
    }

    public async Task<RoleRecord?> FindByIdAsync(
        string roleId, CancellationToken cancellationToken = default)
    {
        var role = await _roles.FindByIdAsync(roleId);

        return role is null ? null : await ToRecordAsync(role);
    }

    public async Task<RoleRecord?> FindByNameAsync(
        string name, CancellationToken cancellationToken = default)
    {
        var role = await _roles.FindByNameAsync(name);

        return role is null ? null : await ToRecordAsync(role);
    }

    public async Task<RoleWriteResult> CreateAsync(
        string name, string? description, CancellationToken cancellationToken = default)
    {
        var role = new ApplicationRole(name) { Description = description };

        var created = await _roles.CreateAsync(role);

        return created.Succeeded
            ? RoleWriteResult.Ok(await ToRecordAsync(role))
            : RoleWriteResult.Refused(Describe(created));
    }

    public async Task<RoleWriteResult> RenameAsync(
        string roleId, string name, string? description, CancellationToken cancellationToken = default)
    {
        var role = await _roles.FindByIdAsync(roleId);
        if (role is null) return RoleWriteResult.Refused("No role has that id.");

        role.Name = name;
        role.Description = description;

        var updated = await _roles.UpdateAsync(role);

        return updated.Succeeded
            ? RoleWriteResult.Ok(await ToRecordAsync(role))
            : RoleWriteResult.Refused(Describe(updated));
    }

    public async Task<RoleWriteResult> DeleteAsync(
        string roleId, CancellationToken cancellationToken = default)
    {
        var role = await _roles.FindByIdAsync(roleId);
        if (role is null) return RoleWriteResult.Refused("No role has that id.");

        var record = await ToRecordAsync(role);
        var deleted = await _roles.DeleteAsync(role);

        return deleted.Succeeded
            ? RoleWriteResult.Ok(record)
            : RoleWriteResult.Refused(Describe(deleted));
    }

    public async Task SetPermissionsAsync(
        string roleId, IReadOnlySet<string> permissions, CancellationToken cancellationToken = default)
    {
        var role = await _roles.FindByIdAsync(roleId);
        if (role is null) return;

        var current = await CurrentPermissionsAsync(role);

        foreach (var permission in permissions.Except(current))
        {
            await _roles.AddClaimAsync(role, new Claim(PermissionClaimTypes.Permission, permission));
        }

        foreach (var permission in current.Except(permissions))
        {
            await _roles.RemoveClaimAsync(role, new Claim(PermissionClaimTypes.Permission, permission));
        }
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _db.SaveChangesAsync(cancellationToken);

    private async Task<RoleRecord> ToRecordAsync(ApplicationRole role)
    {
        var permissions = await CurrentPermissionsAsync(role);

        return new RoleRecord(
            role.Id,
            role.Name ?? string.Empty,
            role.Description,
            permissions.OrderBy(p => p, StringComparer.Ordinal).ToList(),
            role.Name is null ? 0 : (await _users.GetUsersInRoleAsync(role.Name)).Count);
    }

    private async Task<HashSet<string>> CurrentPermissionsAsync(ApplicationRole role)
    {
        var claims = await _roles.GetClaimsAsync(role);

        return claims
            .Where(c => c.Type == PermissionClaimTypes.Permission)
            .Select(c => c.Value)
            .ToHashSet(StringComparer.Ordinal);
    }

    private static string Describe(IdentityResult result) =>
        string.Join(" ", result.Errors.Select(e => e.Description));
}
