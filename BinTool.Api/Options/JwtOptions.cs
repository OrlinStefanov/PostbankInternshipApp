namespace BinTool.Api.Options;

/// <summary>
/// Signing and validation settings for the access tokens the API issues.
/// Bound from the <c>Jwt</c> configuration section.
/// </summary>
public class JwtOptions
{
    public const string SectionName = "Jwt";

    /// <summary>
    /// Minimum key length the HMAC-SHA256 signer will accept, in bytes. A shorter key
    /// weakens the signature, so a misconfiguration should fail loudly rather than
    /// quietly produce forgeable tokens.
    /// </summary>
    public const int MinimumKeyBytes = 32;

    public string Issuer { get; set; } = "BinTool";

    public string Audience { get; set; } = "BinTool";

    /// <summary>
    /// Symmetric signing key. Supplied through configuration - user secrets or an
    /// environment variable outside development.
    /// </summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// How long an issued token stays valid. There is no refresh token: when it expires
    /// the user logs in again.
    /// </summary>
    public int ExpiryMinutes { get; set; } = 60;
}
