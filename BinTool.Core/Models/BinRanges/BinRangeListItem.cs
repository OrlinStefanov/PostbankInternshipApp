namespace BinTool.Core.Models.BinRanges;

/// <summary>
/// One BIN range as it appears in a browse listing, with the lookup ids already
/// resolved to their names.
/// </summary>
public class BinRangeListItem
{
    public int BinRangeId { get; set; }

    /// <summary>
    /// BIN prefix, 6 to 8 digits.
    /// </summary>
    public string Prefix { get; set; } = string.Empty;

    /// <summary>
    /// Number of digits in the prefix. A longer prefix wins during classification.
    /// </summary>
    public int PrefixLength { get; set; }

    public string CardScheme { get; set; } = string.Empty;

    public string ProductType { get; set; } = string.Empty;

    public string FundingType { get; set; } = string.Empty;

    /// <summary>
    /// ISO 3166-1 alpha-2 code of the issuing country.
    /// </summary>
    public string CountryCode { get; set; } = string.Empty;

    public string CountryName { get; set; } = string.Empty;

    /// <summary>
    /// Region the issuing country belongs to.
    /// </summary>
    public string Region { get; set; } = string.Empty;

    public DateTime ValidFrom { get; set; }

    /// <summary>
    /// Null for an open-ended range.
    /// </summary>
    public DateTime? ValidTo { get; set; }

    /// <summary>
    /// Derived from the validity dates and the soft-delete flag, not stored.
    /// </summary>
    public BinRangeStatus Status { get; set; }

    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// Who last touched the record. Currently the literal "system" for anything an
    /// import wrote, until authentication is wired in.
    /// </summary>
    public string? UpdatedBy { get; set; }
}
