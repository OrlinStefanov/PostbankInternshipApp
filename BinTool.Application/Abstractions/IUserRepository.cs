namespace BinTool.Application.Abstractions;

public interface IUserRepository
{
    Task<IReadOnlyList<UserRecord>> ListAsync(CancellationToken cancellationToken = default);

    Task<UserRecord?> FindByIdAsync(string userId, CancellationToken cancellationToken = default);

    Task<bool> RoleExistsAsync(string roleName, CancellationToken cancellationToken = default);

    Task<int> CountUsersInRoleAsync(string roleName, CancellationToken cancellationToken = default);

    Task AddToRolesAsync(
        string userId, IReadOnlyCollection<string> roleNames, CancellationToken cancellationToken = default);

    Task RemoveFromRolesAsync(
        string userId, IReadOnlyCollection<string> roleNames, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}

public sealed record UserRecord(
    string Id,
    string UserName,
    string? FullName,
    string? Email,
    bool IsActive,
    IReadOnlyList<string> Roles);
