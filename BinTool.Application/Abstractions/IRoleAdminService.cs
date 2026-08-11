using BinTool.Application.Authorization;
using BinTool.Application.Models.Access;

namespace BinTool.Application.Abstractions;

/// <summary>
/// Manages roles and the permissions they grant. The Admin role is protected: it cannot be
/// renamed, deleted or have its permissions changed, and it holds every permission implicitly.
/// </summary>
public interface IRoleAdminService
{
    /// <summary>All roles, with their permissions and how many users hold each.</summary>
    Task<IReadOnlyList<RoleListItem>> GetRolesAsync(CancellationToken cancellationToken = default);

    /// <summary>The fixed catalog of permissions a role can be composed from.</summary>
    IReadOnlyList<PermissionInfo> GetPermissionCatalog();

    Task<RoleMutationResult> CreateAsync(RoleInput input, CancellationToken cancellationToken = default);

    Task<RoleMutationResult> UpdateAsync(
        string roleId, RoleInput input, CancellationToken cancellationToken = default);

    Task<RoleMutationResult> DeleteAsync(string roleId, CancellationToken cancellationToken = default);
}
