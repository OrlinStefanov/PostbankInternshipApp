using System.ComponentModel.DataAnnotations;

namespace BinTool.Application.Models.ReferenceData;

public class LookupInput
{
    /// <summary>Display name.</summary>
    [Required(ErrorMessage = "Name is required.")]
    [StringLength(100, MinimumLength = 1, ErrorMessage = "Name must be 1 to 100 characters.")]
    public string Name { get; set; } = string.Empty;

    /// <summary>Optional free-text description.</summary>
    [StringLength(500, ErrorMessage = "Description must be at most 500 characters.")]
    public string? Description { get; set; }
}
