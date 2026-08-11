using Microsoft.AspNetCore.Identity;

namespace BinTool.Domain.Entities;

// Application user, backed by ASP.NET Core Identity. Identity supplies Id, UserName, Email,
// PasswordHash, lockout and 2FA columns; the properties below are the BinTool-specific additions.
// Stored in the Identity-generated AspNetUsers table.
public class ApplicationUser : IdentityUser
{
    public string FullName { get; set; } = string.Empty;

    // Whether the account is enabled. Disabled accounts cannot sign in.
    public bool IsActive { get; set; } = true;

    public string? Notes { get; set; }

    #region Audit Fields

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? LastLoginAt { get; set; }

    #endregion

    #region Navigation Properties

    public ICollection<AuditEntry> AuditEntries { get; set; } = new List<AuditEntry>();

    public ICollection<ImportHistory> Imports { get; set; } = new List<ImportHistory>();

    #endregion
}
