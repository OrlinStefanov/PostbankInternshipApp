using Microsoft.AspNetCore.Identity;

namespace BinTool.Domain.Entities;

public class ApplicationRole : IdentityRole
{
    public ApplicationRole()
    {
    }

    public ApplicationRole(string roleName) : base(roleName)
    {
    }

    public string? Description { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

// Canonical role names. Use these constants instead of string literals so [Authorize(Roles = ...)]
// attributes stay in sync with the seeded roles.
public static class AppRoles
{
    public const string Admin = "Admin";

    public const string Viewer = "Viewer";

    public static readonly IReadOnlyList<string> All = new[] { Admin, Viewer };
}
