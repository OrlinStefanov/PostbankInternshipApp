using System.ComponentModel.DataAnnotations;

namespace BinTool.Application.Models.BinRanges;

/// <summary>
/// The values supplied when an admin adds or edits a single BIN range by hand.
/// <para>
/// Reference data is named rather than given by id, so a hand-written range uses the
/// same vocabulary as a CSV row and as the browse listing. Names are matched
/// case-insensitively and must already exist - this never creates reference data.
/// </para>
/// </summary>
public class BinRangeInput : IValidatableObject
{
    /// <summary>
    /// BIN prefix, 6 to 8 digits. Must not already belong to another range.
    /// </summary>
    [Required(ErrorMessage = "Prefix is required.")]
    [RegularExpression(@"^\d{6,8}$", ErrorMessage = "Prefix must be 6 to 8 digits.")]
    public string Prefix { get; set; } = string.Empty;

    [Required(ErrorMessage = "CardScheme is required.")]
    public string CardScheme { get; set; } = string.Empty;

    [Required(ErrorMessage = "ProductType is required.")]
    public string ProductType { get; set; } = string.Empty;

    [Required(ErrorMessage = "FundingType is required.")]
    public string FundingType { get; set; } = string.Empty;

    /// <summary>
    /// ISO 3166-1 alpha-2 code of the issuing country.
    /// </summary>
    [Required(ErrorMessage = "CountryCode is required.")]
    [RegularExpression("^[A-Za-z]{2}$", ErrorMessage = "CountryCode must be a 2-letter ISO code.")]
    public string CountryCode { get; set; } = string.Empty;

    /// <summary>
    /// First day the range is valid, inclusive.
    /// </summary>
    public DateTime ValidFrom { get; set; }

    /// <summary>
    /// Last day the range is valid, inclusive. Null leaves the range open-ended.
    /// </summary>
    public DateTime? ValidTo { get; set; }

    /// <summary>
    /// Set to true to save even when the prefix's detected network does not match
    /// <see cref="CardScheme"/>. False on the first attempt so the caller sees the warning;
    /// true on the second so the caller is on record as having chosen to override it.
    /// </summary>
    public bool AcknowledgeSchemeMismatch { get; set; }

    /// <summary>
    /// The rules that span more than one field. Kept on the model rather than in the
    /// service so the API's automatic model validation and a direct service call reject
    /// exactly the same input.
    /// </summary>
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        // A missing date binds to default(DateTime) rather than failing, so [Required]
        // would never fire on a non-nullable DateTime.
        if (ValidFrom == default)
        {
            yield return new ValidationResult(
                "ValidFrom is required.", new[] { nameof(ValidFrom) });
        }

        if (ValidTo is not null && ValidTo <= ValidFrom)
        {
            yield return new ValidationResult(
                "ValidTo must be after ValidFrom.", new[] { nameof(ValidTo) });
        }
    }
}
