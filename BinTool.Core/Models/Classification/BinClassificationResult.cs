namespace BinTool.Core.Models.Classification;

/// <summary>
/// The outcome of classifying a BIN. When <see cref="Matched"/> is false the card
/// attributes are all null - no BIN range covers the prefix, so nothing is known
/// about the card beyond the digits that were supplied.
/// </summary>
public class BinClassificationResult
{
    /// <summary>
    /// The lookup key actually used: the leading digits of the input, at most 8 of them.
    /// A longer card number is truncated to this before the lookup runs.
    /// </summary>
    public string Bin { get; set; } = string.Empty;

    /// <summary>
    /// True when a BIN range covers this prefix and is valid today.
    /// </summary>
    public bool Matched { get; set; }

    /// <summary>
    /// The prefix of the BIN range that matched, which may be shorter than <see cref="Bin"/>.
    /// Null when nothing matched.
    /// </summary>
    public string? MatchedPrefix { get; set; }

    /// <summary>
    /// Card scheme name, for example "Visa".
    /// </summary>
    public string? CardScheme { get; set; }

    /// <summary>
    /// Product type name, for example "Consumer".
    /// </summary>
    public string? ProductType { get; set; }

    /// <summary>
    /// Funding type name, for example "Credit".
    /// </summary>
    public string? FundingType { get; set; }

    /// <summary>
    /// ISO 3166-1 alpha-2 code of the issuing country.
    /// </summary>
    public string? CountryCode { get; set; }

    /// <summary>
    /// Full name of the issuing country.
    /// </summary>
    public string? CountryName { get; set; }

    /// <summary>
    /// Region the issuing country belongs to - the input to commission rule resolution.
    /// </summary>
    public string? Region { get; set; }

    /// <summary>
    /// Start of the matched range's validity (inclusive).
    /// </summary>
    public DateTime? ValidFrom { get; set; }

    /// <summary>
    /// End of the matched range's validity (inclusive). Null for an open-ended range.
    /// </summary>
    public DateTime? ValidTo { get; set; }
}
