using System.ComponentModel.DataAnnotations;

namespace BinTool.Application.Models.Auth;

/// <summary>Credentials for an access-token request.</summary>
public class LoginRequest
{
    /// <summary>User name or email address.</summary>
    /// <example>admin</example>
    [Required(ErrorMessage = "A user name is required.")]
    public string UserName { get; set; } = string.Empty;

    [Required(ErrorMessage = "A password is required.")]
    public string Password { get; set; } = string.Empty;
}
