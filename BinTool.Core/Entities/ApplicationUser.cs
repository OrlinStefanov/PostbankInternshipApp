using Microsoft.AspNetCore.Identity;

namespace BinTool.Core.Entities;

/// <summary>
/// Application user entity for ASP.NET Core Identity
/// Extend this class with additional properties as needed
/// </summary>
public class ApplicationUser : IdentityUser
{
    /// <summary>
    /// Full display name of the user
    /// </summary>
    public string? FullName { get; set; }

    /// <summary>
    /// Whether this user account is active
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// When the user was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the user was last modified
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Notes about this user (internal use)
    /// </summary>
    public string? Notes { get; set; }
}
