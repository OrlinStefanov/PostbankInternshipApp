namespace BinTool.Domain.Common;

public static class CardSchemeDetectorExtensions
{
    public static string? MismatchedName(
        this ICardSchemeDetector detector, string prefix, string storedScheme)
    {
        var detected = detector.Detect(prefix);
        var detectedName = detector.DisplayName(detected);
        if (detectedName is null) return null;

        return detector.Matches(detected, storedScheme) ? null : detectedName;
    }
}
