using BinTool.Application.Models.Access;

namespace BinTool.Application.Abstractions;

public interface IUserAdminService
{
    Task<IReadOnlyList<UserListItem>> GetUsersAsync(CancellationToken cancellationToken = default);

    /// <summary>Replaces a user's roles with the supplied set.</summary>
    Task<UserRolesResult> SetRolesAsync(
        string userId, UserRolesInput input, CancellationToken cancellationToken = default);
}
