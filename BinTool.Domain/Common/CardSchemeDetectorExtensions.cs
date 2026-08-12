namespace BinTool.Domain.Common;

public static class CardSchemeDetectorExtensions
{
    /// <summary>
    /// The network the prefix's digits say it belongs to, when that contradicts the scheme stored
    /// against it - and null when there is nothing to report.
    /// </summary>
    public static string? MismatchedName(
        this ICardSchemeDetector detector, string prefix, string storedScheme)
    {
        var detected = detector.Detect(prefix);
        var detectedName = detector.DisplayName(detected);
        if (detectedName is null) return null;

        return detector.Matches(detected, storedScheme) ? null : detectedName;
    }
}
