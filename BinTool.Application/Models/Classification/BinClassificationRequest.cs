using System.ComponentModel.DataAnnotations;

namespace BinTool.Application.Models.Classification;

public class BinClassificationRequest
{
    /// <summary>
    /// The BIN, or a full card number to take it from: 6 to 19 digits with no spaces, dashes or
    /// other separators.
    /// </summary>
    [Required(ErrorMessage = "A BIN is required.")]
    [RegularExpression(@"^\d{6,19}$",
        ErrorMessage = "The BIN must be 6 to 19 digits, with no spaces or separators.")]
    public string Bin { get; set; } = string.Empty;

    /// <summary>An optional transaction amount.</summary>
    [Range(0, 1_000_000_000_000, ErrorMessage = "The amount must be between 0 and 1,000,000,000,000.")]
    public decimal? Amount { get; set; }

    /// <summary>The ISO-4217 code the <see cref="Amount"/> is given in, e.g. "EUR".</summary>
    [RegularExpression("^[A-Za-z]{3}$", ErrorMessage = "The currency code must be three letters.")]
    public string? AmountCurrency { get; set; }
}
