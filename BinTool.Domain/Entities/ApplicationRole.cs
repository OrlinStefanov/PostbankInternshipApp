using Microsoft.AspNetCore.Identity;

namespace BinTool.Domain.Entities;

/// <summary>
/// Application role, backed by ASP.NET Core Identity.
/// Stored in the Identity-generated AspNetRoles table.
/// Role names are defined in <see cref="AppRoles"/>.
/// </summary>
public class ApplicationRole : IdentityRole
{
    public ApplicationRole()
    {
    }

    public ApplicationRole(string roleName) : base(roleName)
    {
    }

    /// <summary>
    /// Human-readable explanation of what the role is allowed to do
    /// </summary>
    public string? Description { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Canonical role names. Use these constants instead of string literals
/// so [Authorize(Roles = ...)] attributes stay in sync with the seeded roles.
/// </summary>
public static class AppRoles
{
    /// <summary>
    /// Full access: manage BIN ranges, commission rules, users and imports
    /// </summary>
    public const string Admin = "Admin";

    /// <summary>
    /// Read-only access: browse BIN ranges, rules and audit history
    /// </summary>
    public const string Viewer = "Viewer";

    public static readonly IReadOnlyList<string> All = new[] { Admin, Viewer };
}
