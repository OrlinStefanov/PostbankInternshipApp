using System.ComponentModel.DataAnnotations;

namespace BinTool.Application.Models.Auth;

public class LoginRequest
{
    [Required(ErrorMessage = "A user name is required.")]
    public string UserName { get; set; } = string.Empty;

    [Required(ErrorMessage = "A password is required.")]
    public string Password { get; set; } = string.Empty;
}
