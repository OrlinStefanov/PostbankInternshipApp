using System.ComponentModel.DataAnnotations;

namespace BinTool.Application.Models.Classification;

/// <summary>A request to classify a single BIN.</summary>
public class BinClassificationRequest
{
    /// <summary>
    /// The BIN, or a full card number to take it from: 6 to 19 digits with no spaces,
    /// dashes or other separators. Only the leading 8 digits are used for the lookup -
    /// the rest is discarded before anything is queried, stored or logged.
    /// </summary>
    /// <example>400001</example>
    [Required(ErrorMessage = "A BIN is required.")]
    [RegularExpression(@"^\d{6,19}$",
        ErrorMessage = "The BIN must be 6 to 19 digits, with no spaces or separators.")]
    public string Bin { get; set; } = string.Empty;

    /// <summary>
    /// An optional transaction amount. When supplied, the response also carries the
    /// commission worked out for it: the applicable rule is resolved and the fee is
    /// calculated. Omit it to classify the card without pricing.
    /// </summary>
    /// <example>100</example>
    [Range(0, 1_000_000_000_000, ErrorMessage = "The amount must be between 0 and 1,000,000,000,000.")]
    public decimal? Amount { get; set; }

    /// <summary>
    /// The ISO-4217 code the <see cref="Amount"/> is given in, e.g. "EUR". When the applied
    /// rule is priced in a different currency the amount is converted before the fee is
    /// worked out. Defaults to euro when omitted or unrecognised.
    /// </summary>
    /// <example>EUR</example>
    [RegularExpression("^[A-Za-z]{3}$", ErrorMessage = "The currency code must be three letters.")]
    public string? AmountCurrency { get; set; }
}
