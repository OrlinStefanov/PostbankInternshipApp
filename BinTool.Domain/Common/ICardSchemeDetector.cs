namespace BinTool.Domain.Common;

public enum DetectedScheme
{
    Unknown = 0,
    Visa,
    Mastercard,
    AmericanExpress,
    DinersClub,
    JCB,
    Discover,
    UnionPay
}

public interface ICardSchemeDetector
{
    /// <summary>
    /// The network the prefix belongs to by its digits alone, or <see
    /// cref="DetectedScheme.Unknown"/> when it matches no known range.
    /// </summary>
    DetectedScheme Detect(string prefix);

    /// <summary>
    /// Whether a declared card-scheme name is the network <paramref name="detected"/> stands for.
    /// </summary>
    bool Matches(DetectedScheme detected, string? declaredSchemeName);

    /// <summary>
    /// A human name for a detected network ("Visa", "Mastercard", …), or null for <see
    /// cref="DetectedScheme.Unknown"/>.
    /// </summary>
    string? DisplayName(DetectedScheme detected);
}
