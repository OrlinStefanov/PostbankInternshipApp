namespace BinTool.Domain.Common;

public static class BinPrefix
{
    // Shortest stored prefix, so anything shorter cannot be classified at all.
    public const int MinLength = 6;

    // Longest stored prefix. Also the point at which the input is truncated: the tool never
    // needs more of a card number than this, so it never holds more.
    public const int MaxLength = 8;

    // Longest PAN under ISO/IEC 7812. Anything longer is not a card number.
    public const int MaxInputLength = 19;

    /// <summary>
    /// Trims, checks and truncates an incoming BIN or full card number to the lookup key.
    /// </summary>
    public static string Normalize(string value, string paramName = "bin")
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("A BIN is required.", paramName);
        }

        var trimmed = value.Trim();

        foreach (var c in trimmed)
        {
            if (!char.IsAsciiDigit(c))
            {
                throw new ArgumentException("The BIN must contain digits only.", paramName);
            }
        }

        if (trimmed.Length < MinLength)
        {
            throw new ArgumentException($"The BIN must be at least {MinLength} digits.", paramName);
        }

        if (trimmed.Length > MaxInputLength)
        {
            throw new ArgumentException($"The BIN must be at most {MaxInputLength} digits.", paramName);
        }

        return trimmed.Length > MaxLength ? trimmed[..MaxLength] : trimmed;
    }

    /// <summary>Every stored prefix length the key could match, shortest first.</summary>
    public static string[] Candidates(string lookupKey)
    {
        var candidates = new string[lookupKey.Length - MinLength + 1];

        for (var length = MinLength; length <= lookupKey.Length; length++)
        {
            candidates[length - MinLength] = lookupKey[..length];
        }

        return candidates;
    }
}
