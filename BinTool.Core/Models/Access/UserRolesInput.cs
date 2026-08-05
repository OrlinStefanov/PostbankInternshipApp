namespace BinTool.Core.Models.Access;

/// <summary>
/// The complete set of roles a user should hold after the change. It is a replacement, not a
/// delta: whatever is not listed is removed, so the screen sends the checked boxes as-is.
/// </summary>
public class UserRolesInput
{
    public List<string> Roles { get; set; } = new();
}
