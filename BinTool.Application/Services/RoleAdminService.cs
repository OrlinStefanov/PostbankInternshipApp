using BinTool.Application.Abstractions;
using BinTool.Application.Authorization;
using BinTool.Application.Mapping;
using BinTool.Application.Models.Access;
using BinTool.Application.Models.Audit;
using Microsoft.Extensions.Logging;

namespace BinTool.Application.Services;

public class RoleAdminService : IRoleAdminService
{
    private readonly IRoleRepository _roles;
    private readonly ICurrentUser _currentUser;
    private readonly IAuditLog _audit;
    private readonly ILogger<RoleAdminService> _logger;

    public RoleAdminService(
        IRoleRepository roles,
        ICurrentUser currentUser,
        IAuditLog audit,
        ILogger<RoleAdminService> logger)
    {
        _roles = roles;
        _currentUser = currentUser;
        _audit = audit;
        _logger = logger;
    }

    public IReadOnlyList<PermissionInfo> GetPermissionCatalog() => Permissions.All;

    public async Task<IReadOnlyList<RoleListItem>> GetRolesAsync(
        CancellationToken cancellationToken = default)
    {
        var roles = await _roles.ListAsync(cancellationToken);

        return roles
            .Select(RoleMapper.ToListItem)
            // Protected first, then alphabetical - the god role belongs at the top.
            .OrderByDescending(r => r.IsProtected)
            .ThenBy(r => r.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public async Task<RoleMutationResult> CreateAsync(
        RoleInput input, CancellationToken cancellationToken = default)
    {
        var name = input.NormalizedName();

        if (await _roles.FindByNameAsync(name, cancellationToken) is not null)
        {
            return Refused(RoleMutationResult.Failure(
                RoleMutationStatus.NameInUse, $"A role named '{name}' already exists."), name);
        }

        var created = await _roles.CreateAsync(
            name, input.NormalizedDescription(), cancellationToken);

        if (!created.Succeeded)
        {
            return Refused(RoleMutationResult.Failure(
                RoleMutationStatus.Invalid, created.Error), name);
        }

        var role = created.Role!;

        await _roles.SetPermissionsAsync(role.Id, input.WantedPermissions(), cancellationToken);

        var saved = await _roles.FindByIdAsync(role.Id, cancellationToken) ?? role;

        _audit.Record(AuditAction.Created, AuditEntityTypes.Role, 0,
            null, RoleMapper.ToSnapshot(saved));

        await _roles.SaveChangesAsync(cancellationToken);

        RoleLog.Created(_logger, saved.Id, saved.Name, saved.Permissions.Count, _currentUser.Name);

        return RoleMutationResult.Success(
            RoleMutationStatus.Created, RoleMapper.ToListItem(saved));
    }

    public async Task<RoleMutationResult> UpdateAsync(
        string roleId, RoleInput input, CancellationToken cancellationToken = default)
    {
        var role = await _roles.FindByIdAsync(roleId, cancellationToken);
        if (role is null) return NotFound(roleId);

        if (RoleMapper.IsProtected(role.Name))
        {
            return Refused(RoleMutationResult.Failure(
                RoleMutationStatus.Protected,
                "The Admin role is protected: it can't be renamed, deleted or have its " +
                "permissions changed."), role.Name);
        }

        var name = input.NormalizedName();

        // Renaming onto a name another role owns would collide; keeping its own name is fine.
        var owner = await _roles.FindByNameAsync(name, cancellationToken);
        if (owner is not null && owner.Id != role.Id)
        {
            return Refused(RoleMutationResult.Failure(
                RoleMutationStatus.NameInUse, $"A role named '{name}' already exists."), name);
        }

        var before = RoleMapper.ToSnapshot(role);

        var renamed = await _roles.RenameAsync(
            roleId, name, input.NormalizedDescription(), cancellationToken);

        if (!renamed.Succeeded)
        {
            return Refused(RoleMutationResult.Failure(
                RoleMutationStatus.Invalid, renamed.Error), name);
        }

        await _roles.SetPermissionsAsync(roleId, input.WantedPermissions(), cancellationToken);

        var saved = await _roles.FindByIdAsync(roleId, cancellationToken) ?? renamed.Role!;

        _audit.Record(AuditAction.Updated, AuditEntityTypes.Role, 0,
            before, RoleMapper.ToSnapshot(saved));

        await _roles.SaveChangesAsync(cancellationToken);

        RoleLog.Updated(_logger, saved.Id, saved.Name, saved.Permissions.Count, _currentUser.Name);

        return RoleMutationResult.Success(
            RoleMutationStatus.Updated, RoleMapper.ToListItem(saved));
    }

    public async Task<RoleMutationResult> DeleteAsync(
        string roleId, CancellationToken cancellationToken = default)
    {
        var role = await _roles.FindByIdAsync(roleId, cancellationToken);
        if (role is null) return NotFound(roleId);

        if (RoleMapper.IsProtected(role.Name))
        {
            return Refused(RoleMutationResult.Failure(
                RoleMutationStatus.Protected,
                "The Admin role is protected and can't be deleted."), role.Name);
        }

        if (role.MemberCount > 0)
        {
            return Refused(RoleMutationResult.Failure(
                RoleMutationStatus.InUse,
                $"This role is held by {role.MemberCount} " +
                $"{(role.MemberCount == 1 ? "user" : "users")}. " +
                "Remove it from them before deleting it."), role.Name);
        }

        var before = RoleMapper.ToSnapshot(role);

        var deleted = await _roles.DeleteAsync(roleId, cancellationToken);
        if (!deleted.Succeeded)
        {
            return Refused(RoleMutationResult.Failure(
                RoleMutationStatus.Invalid, deleted.Error), role.Name);
        }

        _audit.Record(AuditAction.Deleted, AuditEntityTypes.Role, 0, before, null);

        await _roles.SaveChangesAsync(cancellationToken);

        RoleLog.Deleted(_logger, roleId, role.Name, _currentUser.Name);

        return RoleMutationResult.Success(RoleMutationStatus.Deleted);
    }

    private RoleMutationResult Refused(RoleMutationResult result, string roleName)
    {
        RoleLog.WriteRefused(
            _logger, roleName, result.Status.ToString(), result.Error ?? string.Empty);

        return result;
    }

    private RoleMutationResult NotFound(string roleId) =>
        Refused(RoleMutationResult.Failure(
            RoleMutationStatus.NotFound, "No role has that id."), roleId);
}
