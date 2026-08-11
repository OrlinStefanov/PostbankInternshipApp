using System.ComponentModel.DataAnnotations;

namespace BinTool.Application.Models.ReferenceData;

/// <summary>
/// The values supplied when an admin adds or edits a country. The <see cref="IsoCode"/>
/// is case-insensitively unique across live and soft-deleted rows.
/// </summary>
public class CountryInput
{
    /// <summary>ISO 3166-1 alpha-2 country code. Stored uppercased.</summary>
    [Required(ErrorMessage = "IsoCode is required.")]
    [RegularExpression("^[A-Za-z]{2}$", ErrorMessage = "IsoCode must be a 2-letter ISO code.")]
    public string IsoCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "Name is required.")]
    [StringLength(100, MinimumLength = 1, ErrorMessage = "Name must be 1 to 100 characters.")]
    public string Name { get; set; } = string.Empty;

    /// <summary>Region the country belongs to. Must reference a live region.</summary>
    [Range(1, int.MaxValue, ErrorMessage = "RegionId must be a positive number.")]
    public int RegionId { get; set; }
}
