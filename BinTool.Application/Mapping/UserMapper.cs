using BinTool.Application.Models.Access;

namespace BinTool.Application.Mapping;

public static class UserMapper
{
    public static IReadOnlySet<string> WantedRoles(this UserRolesInput input) =>
        input.Roles
            .Select(r => r.Trim())
            .Where(r => r.Length > 0)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

    public static UserListItem ToListItem(UserRecord user) => new()
    {
        Id = user.Id,
        UserName = user.UserName,
        FullName = user.FullName,
        Email = user.Email,
        IsActive = user.IsActive,
        Roles = user.Roles.ToList()
    };

    public static object ToRolesSnapshot(UserRecord user, string[] roles) => new
    {
        UserId = user.Id,
        user.UserName,
        Roles = roles
    };
}
