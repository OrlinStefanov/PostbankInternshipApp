namespace BinTool.Api.Options;

public class JwtOptions
{
    public const string SectionName = "Jwt";

    public const int MinimumKeyBytes = 32;

    public string Issuer { get; set; } = "BinTool";

    public string Audience { get; set; } = "BinTool";

    public string Key { get; set; } = string.Empty;

    public int ExpiryMinutes { get; set; } = 60;
}
