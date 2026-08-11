namespace BinTool.Api.Options;

public class JwtOptions
{
    public const string SectionName = "Jwt";

    // Minimum key length the HMAC-SHA256 signer will accept, in bytes. A shorter key weakens the
    // signature, so a misconfiguration should fail loudly rather than quietly produce forgeable
    // tokens.
    public const int MinimumKeyBytes = 32;

    public string Issuer { get; set; } = "BinTool";

    public string Audience { get; set; } = "BinTool";

    public string Key { get; set; } = string.Empty;

    public int ExpiryMinutes { get; set; } = 60;
}
