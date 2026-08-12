using System.ComponentModel.DataAnnotations;

namespace BinTool.Application.Models.BinRanges;

public class BinRangeInput : IValidatableObject
{
    /// <summary>BIN prefix, 6 to 8 digits.</summary>
    [Required(ErrorMessage = "Prefix is required.")]
    [RegularExpression(@"^\d{6,8}$", ErrorMessage = "Prefix must be 6 to 8 digits.")]
    public string Prefix { get; set; } = string.Empty;

    [Required(ErrorMessage = "CardScheme is required.")]
    public string CardScheme { get; set; } = string.Empty;

    [Required(ErrorMessage = "ProductType is required.")]
    public string ProductType { get; set; } = string.Empty;

    [Required(ErrorMessage = "FundingType is required.")]
    public string FundingType { get; set; } = string.Empty;

    /// <summary>ISO 3166-1 alpha-2 code of the issuing country.</summary>
    [Required(ErrorMessage = "CountryCode is required.")]
    [RegularExpression("^[A-Za-z]{2}$", ErrorMessage = "CountryCode must be a 2-letter ISO code.")]
    public string CountryCode { get; set; } = string.Empty;

    /// <summary>First day the range is valid, inclusive.</summary>
    public DateTime ValidFrom { get; set; }

    /// <summary>Last day the range is valid, inclusive.</summary>
    public DateTime? ValidTo { get; set; }

    /// <summary>
    /// Set to true to save even when the prefix's detected network does not match <see
    /// cref="CardScheme"/>.
    /// </summary>
    public bool AcknowledgeSchemeMismatch { get; set; }

    /// <summary>The rules that span more than one field.</summary>
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
