using BinTool.Application.Models.Access;
using BinTool.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;

namespace BinTool.Infrastructure.Repositories;

public class UserRepository : IUserRepository
{
    private readonly UserManager<ApplicationUser> _users;
    private readonly RoleManager<ApplicationRole> _roles;
    private readonly AppDbContext _db;

    public UserRepository(
        UserManager<ApplicationUser> users,
        RoleManager<ApplicationRole> roles,
        AppDbContext db)
    {
        _users = users;
        _roles = roles;
        _db = db;
    }

    public async Task<IReadOnlyList<UserRecord>> ListAsync(CancellationToken cancellationToken = default)
    {
        var users = await _users.Users.OrderBy(u => u.UserName).ToListAsync(cancellationToken);

        var records = new List<UserRecord>(users.Count);
        foreach (var user in users)
        {
            records.Add(await ToRecordAsync(user));
        }

        return records;
    }

    public async Task<UserRecord?> FindByIdAsync(
        string userId, CancellationToken cancellationToken = default)
    {
        var user = await _users.FindByIdAsync(userId);

        return user is null ? null : await ToRecordAsync(user);
    }

    public Task<bool> RoleExistsAsync(string roleName, CancellationToken cancellationToken = default) =>
        _roles.RoleExistsAsync(roleName);

    public async Task<int> CountUsersInRoleAsync(
        string roleName, CancellationToken cancellationToken = default) =>
        (await _users.GetUsersInRoleAsync(roleName)).Count;

    public async Task AddToRolesAsync(
        string userId, IReadOnlyCollection<string> roleNames, CancellationToken cancellationToken = default)
    {
        if (roleNames.Count == 0) return;

        var user = await _users.FindByIdAsync(userId);
        if (user is null) return;

        await _users.AddToRolesAsync(user, roleNames);
    }

    public async Task RemoveFromRolesAsync(
        string userId, IReadOnlyCollection<string> roleNames, CancellationToken cancellationToken = default)
    {
        if (roleNames.Count == 0) return;

        var user = await _users.FindByIdAsync(userId);
        if (user is null) return;

        await _users.RemoveFromRolesAsync(user, roleNames);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _db.SaveChangesAsync(cancellationToken);

    private async Task<UserRecord> ToRecordAsync(ApplicationUser user) => new(
        user.Id,
        user.UserName ?? string.Empty,
        user.FullName,
        user.Email,
        user.IsActive,
        (await _users.GetRolesAsync(user))
            .OrderBy(r => r, StringComparer.OrdinalIgnoreCase).ToList());
}
