using BinTool.Core.Services;

namespace BinTool.Infrastructure.Services;

/// <summary>
/// Maps a BIN prefix to its card network using the well-known IIN ranges. The rules
/// cover the four schemes the reference data ships with; anything outside them is
/// reported as <see cref="DetectedScheme.Unknown"/> rather than guessed at.
/// <para>
/// Only leading digits are inspected, so a 6-8 digit prefix is classified the same
/// way a full card number would be. The detector is stateless and holds no data of
/// its own, so it is safe to share as a singleton.
/// </para>
/// </summary>
public class CardSchemeDetector : ICardSchemeDetector
{
    // Accepted names per network. The reference data may rename a scheme, so a few
    // common aliases are allowed rather than pinning to one exact string.
    private static readonly Dictionary<DetectedScheme, string[]> Aliases = new()
    {
        [DetectedScheme.Visa] = new[] { "Visa" },
        [DetectedScheme.Mastercard] = new[] { "Mastercard", "Master Card", "MasterCard", "Maestro" },
        [DetectedScheme.AmericanExpress] = new[] { "American Express", "Amex" },
        [DetectedScheme.DinersClub] = new[] { "Diners Club", "Diners", "Diners Club International" },
    };

    public DetectedScheme Detect(string prefix)
    {
        if (string.IsNullOrEmpty(prefix) || !IsAllDigits(prefix))
            return DetectedScheme.Unknown;

        // Compare on numeric ranges of a fixed width rather than string prefixes so
        // the four-digit Mastercard 2-series is tested cleanly alongside the rest.
        var d1 = prefix[0] - '0';
        var two = TakeDigits(prefix, 2);
        var three = TakeDigits(prefix, 3);
        var four = TakeDigits(prefix, 4);

        // Visa: any card starting with 4.
        if (d1 == 4)
            return DetectedScheme.Visa;

        // Mastercard: 51-55, plus the 2221-2720 range added in 2017.
        if (two is >= 51 and <= 55)
            return DetectedScheme.Mastercard;
        if (four is >= 2221 and <= 2720)
            return DetectedScheme.Mastercard;

        // American Express: 34 and 37.
        if (two is 34 or 37)
            return DetectedScheme.AmericanExpress;

        // Diners Club: 300-305, 3095, 36, 38, 39.
        if (three is >= 300 and <= 305 || four == 3095 || two is 36 or 38 or 39)
            return DetectedScheme.DinersClub;

        return DetectedScheme.Unknown;
    }

    public bool Matches(DetectedScheme detected, string? declaredSchemeName)
    {
        if (detected == DetectedScheme.Unknown || string.IsNullOrWhiteSpace(declaredSchemeName))
            return false;

        var declared = declaredSchemeName.Trim();
        return Aliases.TryGetValue(detected, out var names)
            && names.Any(n => string.Equals(n, declared, StringComparison.OrdinalIgnoreCase));
    }

    public string? DisplayName(DetectedScheme detected) => detected switch
    {
        DetectedScheme.Visa => "Visa",
        DetectedScheme.Mastercard => "Mastercard",
        DetectedScheme.AmericanExpress => "American Express",
        DetectedScheme.DinersClub => "Diners Club",
        _ => null
    };

    /// <summary>
    /// The first <paramref name="count"/> digits as an integer, or -1 when the prefix
    /// is shorter than that (so a too-short prefix simply fails every range test).
    /// </summary>
    private static int TakeDigits(string prefix, int count)
    {
        if (prefix.Length < count)
            return -1;

        var value = 0;
        for (var i = 0; i < count; i++)
            value = value * 10 + (prefix[i] - '0');

        return value;
    }

    private static bool IsAllDigits(string value)
    {
        foreach (var c in value)
        {
            if (!char.IsDigit(c)) return false;
        }

        return true;
    }
}
