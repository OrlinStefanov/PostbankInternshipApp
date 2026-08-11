using BinTool.Application.Models.Access;

namespace BinTool.Application.Abstractions;

/// <summary>
/// Lists users and assigns roles to them. It does not create accounts or touch passwords.
/// <para>
/// Two rules protect the god account: the last admin cannot have Admin removed, and no one can
/// strip their own Admin role.
/// </para>
/// </summary>
public interface IUserAdminService
{
    Task<IReadOnlyList<UserListItem>> GetUsersAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Replaces a user's roles with the supplied set. Roles not listed are removed.
    /// </summary>
    Task<UserRolesResult> SetRolesAsync(
        string userId, UserRolesInput input, CancellationToken cancellationToken = default);
}
