using System.ComponentModel.DataAnnotations;

namespace BinTool.Application.Models.ReferenceData;

public class CountryInput
{
    [Required(ErrorMessage = "IsoCode is required.")]
    [RegularExpression("^[A-Za-z]{2}$", ErrorMessage = "IsoCode must be a 2-letter ISO code.")]
    public string IsoCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "Name is required.")]
    [StringLength(100, MinimumLength = 1, ErrorMessage = "Name must be 1 to 100 characters.")]
    public string Name { get; set; } = string.Empty;

    [Range(1, int.MaxValue, ErrorMessage = "RegionId must be a positive number.")]
    public int RegionId { get; set; }
}
