namespace BinTool.Core.Services;

/// <summary>
/// The card network a BIN prefix belongs to, derived from the published IIN/BIN
/// ranges. <see cref="Unknown"/> means the prefix falls outside every range this
/// detector knows about.
/// </summary>
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

/// <summary>
/// Works out which card network a BIN prefix actually belongs to, independent of
/// whatever a data file claims. Used to catch an import that labels, say, a
/// Mastercard prefix as Visa.
/// </summary>
public interface ICardSchemeDetector
{
    /// <summary>
    /// The network the prefix belongs to by its digits alone, or
    /// <see cref="DetectedScheme.Unknown"/> when it matches no known range.
    /// </summary>
    DetectedScheme Detect(string prefix);

    /// <summary>
    /// Whether a declared card-scheme name is the network <paramref name="detected"/>
    /// stands for. Matching is by name (and common aliases), case-insensitive, so it
    /// works against whatever the reference data calls each scheme. Always false for
    /// <see cref="DetectedScheme.Unknown"/>.
    /// </summary>
    bool Matches(DetectedScheme detected, string? declaredSchemeName);

    /// <summary>
    /// A human name for a detected network ("Visa", "Mastercard", …), or null for
    /// <see cref="DetectedScheme.Unknown"/>.
    /// </summary>
    string? DisplayName(DetectedScheme detected);
}
