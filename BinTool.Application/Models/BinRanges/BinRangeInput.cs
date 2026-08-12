using System.ComponentModel.DataAnnotations;

namespace BinTool.Application.Models.BinRanges;

public class BinRangeInput : IValidatableObject
{
    [Required(ErrorMessage = "Prefix is required.")]
    [RegularExpression(@"^\d{6,8}$", ErrorMessage = "Prefix must be 6 to 8 digits.")]
    public string Prefix { get; set; } = string.Empty;

    [Required(ErrorMessage = "CardScheme is required.")]
    public string CardScheme { get; set; } = string.Empty;

    [Required(ErrorMessage = "ProductType is required.")]
    public string ProductType { get; set; } = string.Empty;

    [Required(ErrorMessage = "FundingType is required.")]
    public string FundingType { get; set; } = string.Empty;

    [Required(ErrorMessage = "CountryCode is required.")]
    [RegularExpression("^[A-Za-z]{2}$", ErrorMessage = "CountryCode must be a 2-letter ISO code.")]
    public string CountryCode { get; set; } = string.Empty;

    public DateTime ValidFrom { get; set; }

    public DateTime? ValidTo { get; set; }

    public bool AcknowledgeSchemeMismatch { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        // [Required] won't fire here.
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
