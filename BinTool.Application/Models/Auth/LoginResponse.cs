namespace BinTool.Application.Models.Auth;

public class LoginResponse
{
    /// <summary>The signed JWT. Send it as <c>Authorization: Bearer &lt;token&gt;</c>.</summary>
    public string AccessToken { get; set; } = string.Empty;

    /// <summary>
    /// When the token stops being accepted, in UTC. After this the caller must log in again; there
    /// is no refresh token.
    /// </summary>
    public DateTime ExpiresAtUtc { get; set; }

    public string UserId { get; set; } = string.Empty;

    public string UserName { get; set; } = string.Empty;

    /// <summary>Display name, for greeting the user in a client.</summary>
    public string FullName { get; set; } = string.Empty;

    /// <summary>
    /// Roles held by the user - what a client should use to decide which screens to offer.
    /// </summary>
    public List<string> Roles { get; set; } = new();

    /// <summary>
    /// The permission keys the user's roles add up to, also carried as claims inside the token.
    /// </summary>
    public List<string> Permissions { get; set; } = new();
}
