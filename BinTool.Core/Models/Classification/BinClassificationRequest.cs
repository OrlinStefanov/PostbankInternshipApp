using System.ComponentModel.DataAnnotations;

namespace BinTool.Core.Models.Classification;

/// <summary>
/// A request to classify a single BIN.
/// </summary>
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
}
