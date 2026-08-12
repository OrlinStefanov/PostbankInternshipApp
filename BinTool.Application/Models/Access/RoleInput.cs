using System.ComponentModel.DataAnnotations;

namespace BinTool.Application.Models.Access;

public class RoleInput : IValidatableObject
{
    [Required(ErrorMessage = "Name is required.")]
    [StringLength(64, MinimumLength = 2, ErrorMessage = "Name must be 2 to 64 characters.")]
    [RegularExpression(@"^[A-Za-z0-9 _-]+$",
        ErrorMessage = "Name may use letters, digits, spaces, hyphens and underscores.")]
    public string Name { get; set; } = string.Empty;

    [StringLength(256, ErrorMessage = "Description must be 256 characters or fewer.")]
    public string? Description { get; set; }

    public List<string> Permissions { get; set; } = new();

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        // Property would shadow it.
        foreach (var key in Permissions.Where(p => !Authorization.Permissions.IsPermission(p)))
        {
            yield return new ValidationResult(
                $"Unknown permission '{key}'.", new[] { nameof(Permissions) });
        }
    }
}
