using BinTool.Application.Abstractions;
using BinTool.Application.Authorization;
using BinTool.Application.Models.Access;

namespace BinTool.Application.Mapping;

public static class RoleMapper
{
    public static string NormalizedName(this RoleInput input) => input.Name.Trim();

    public static string? NormalizedDescription(this RoleInput input) =>
        string.IsNullOrWhiteSpace(input.Description) ? null : input.Description.Trim();

    // Unknown keys are dropped rather than refused: the input model has already rejected
    // them before a real request gets this far, so anything left is a caller bypassing it.
    public static IReadOnlySet<string> WantedPermissions(this RoleInput input) =>
        input.Permissions.Where(Permissions.IsPermission).ToHashSet(StringComparer.Ordinal);

    public static bool IsProtected(string roleName) =>
        string.Equals(roleName, AppRoles.Admin, StringComparison.OrdinalIgnoreCase);

    public static RoleListItem ToListItem(RoleRecord role) => new()
    {
        Id = role.Id,
        Name = role.Name,
        Description = role.Description,
        IsProtected = IsProtected(role.Name),
        MemberCount = role.MemberCount,
        Permissions = role.Permissions.ToList()
    };

    public static object ToSnapshot(RoleRecord role) => new
    {
        RoleId = role.Id,
        role.Name,
        role.Description,
        Permissions = role.Permissions.ToArray()
    };
}
