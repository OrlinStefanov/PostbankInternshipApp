using System.ComponentModel.DataAnnotations;

namespace BinTool.Application.Models.Currency;

public class CurrencyInput
{
    [Required(ErrorMessage = "A currency code is required.")]
    [RegularExpression("^[A-Za-z]{3}$", ErrorMessage = "The currency code must be three letters.")]
    public string Code { get; set; } = string.Empty;

    [Required(ErrorMessage = "A currency name is required.")]
    [StringLength(100, MinimumLength = 1, ErrorMessage = "The name must be 1 to 100 characters.")]
    public string Name { get; set; } = string.Empty;

    [Range(0.0000000001, double.MaxValue, ErrorMessage = "The euro rate must be greater than zero.")]
    public decimal RateToEur { get; set; }

    public bool IsActive { get; set; } = true;
}
