namespace BinTool.Application.Models.BinRanges;

/// <summary>
/// One BIN range as it appears in a browse listing, with the lookup ids already
/// resolved to their names.
/// </summary>
public class BinRangeListItem
{
    public int BinRangeId { get; set; }

    /// <summary>BIN prefix, 6 to 8 digits.</summary>
    public string Prefix { get; set; } = string.Empty;

    /// <summary>
    /// Number of digits in the prefix. A longer prefix wins during classification.
    /// </summary>
    public int PrefixLength { get; set; }

    public string CardScheme { get; set; } = string.Empty;

    public string ProductType { get; set; } = string.Empty;

    public string FundingType { get; set; } = string.Empty;

    /// <summary>ISO 3166-1 alpha-2 code of the issuing country.</summary>
    public string CountryCode { get; set; } = string.Empty;

    public string CountryName { get; set; } = string.Empty;

    /// <summary>Region the issuing country belongs to.</summary>
    public string Region { get; set; } = string.Empty;

    public DateTime ValidFrom { get; set; }

    /// <summary>Null for an open-ended range.</summary>
    public DateTime? ValidTo { get; set; }

    /// <summary>Derived from the validity dates and the soft-delete flag, not stored.</summary>
    public BinRangeStatus Status { get; set; }

    /// <summary>When the range first entered the database.</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// User name of whoever added the range - normally the person who ran the import.
    /// <para>
    /// The literal <c>system</c> means no user was signed in at the time, which is how
    /// rows written before authentication existed are recorded. It is not an account.
    /// </para>
    /// </summary>
    public string? CreatedBy { get; set; }

    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// User name of whoever last changed the range. Differs from <see cref="CreatedBy"/>
    /// once someone applies a conflict that overwrites it.
    /// </summary>
    public string? UpdatedBy { get; set; }

    /// <summary>
    /// The card scheme the detector assigns to this prefix, or null if the prefix sits in
    /// no range the detector recognises. Populated by the scheme-mismatch listing so the
    /// disagreement with <see cref="CardScheme"/> is visible per row; null on the ordinary
    /// browse listing.
    /// </summary>
    public string? DetectedScheme { get; set; }
}
