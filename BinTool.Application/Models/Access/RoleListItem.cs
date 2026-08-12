namespace BinTool.Application.Models.Access;

public class RoleListItem
{
    public string Id { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    /// <summary>True for the built-in Admin role.</summary>
    public bool IsProtected { get; set; }

    /// <summary>How many users currently hold the role.</summary>
    public int MemberCount { get; set; }

    /// <summary>The permission keys the role grants.</summary>
    public List<string> Permissions { get; set; } = new();
}
