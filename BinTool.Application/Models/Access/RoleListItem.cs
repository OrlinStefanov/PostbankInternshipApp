namespace BinTool.Application.Models.Access;

public class RoleListItem
{
    public string Id { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public bool IsProtected { get; set; }

    public int MemberCount { get; set; }

    public List<string> Permissions { get; set; } = new();
}
