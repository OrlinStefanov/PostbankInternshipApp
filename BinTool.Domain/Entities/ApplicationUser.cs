using Microsoft.AspNetCore.Identity;

namespace BinTool.Domain.Entities;

/// <summary>
/// Application user, backed by ASP.NET Core Identity.
/// Identity supplies Id, UserName, Email, PasswordHash, lockout and 2FA columns;
/// the properties below are the BinTool-specific additions.
/// Stored in the Identity-generated AspNetUsers table.
/// </summary>
public class ApplicationUser : IdentityUser
{
    /// <summary>
    /// Display name used in reports and audit screens
    /// </summary>
    public string FullName { get; set; } = string.Empty;

    /// <summary>
    /// Whether the account is enabled. Disabled accounts cannot sign in.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Internal notes about the account (not shown to the user)
    /// </summary>
    public string? Notes { get; set; }

    #region Audit Fields

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Timestamp of the most recent successful sign-in
    /// </summary>
    public DateTime? LastLoginAt { get; set; }

    #endregion

    #region Navigation Properties

    /// <summary>
    /// Audit entries produced by this user
    /// </summary>
    public ICollection<AuditEntry> AuditEntries { get; set; } = new List<AuditEntry>();

    /// <summary>
    /// Bulk imports performed by this user
    /// </summary>
    public ICollection<ImportHistory> Imports { get; set; } = new List<ImportHistory>();

    #endregion
}
