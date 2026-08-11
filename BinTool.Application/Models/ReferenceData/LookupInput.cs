using System.ComponentModel.DataAnnotations;

namespace BinTool.Application.Models.ReferenceData;

/// <summary>
/// The values supplied when an admin adds or edits one of the Name+Description reference
/// rows (card scheme, product type, funding type or region).
/// </summary>
public class LookupInput
{
    /// <summary>
    /// Display name. Case-insensitively unique across live and soft-deleted rows.
    /// </summary>
    [Required(ErrorMessage = "Name is required.")]
    [StringLength(100, MinimumLength = 1, ErrorMessage = "Name must be 1 to 100 characters.")]
    public string Name { get; set; } = string.Empty;

    /// <summary>Optional free-text description.</summary>
    [StringLength(500, ErrorMessage = "Description must be at most 500 characters.")]
    public string? Description { get; set; }
}
