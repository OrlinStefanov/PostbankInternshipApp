namespace BinTool.Application.Models.Access;

public class UserListItem
{
    public string Id { get; set; } = string.Empty;

    public string UserName { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;

    public string? Email { get; set; }

    /// <summary>
    /// Disabled accounts cannot sign in; shown so an admin knows why a user is dark.
    /// </summary>
    public bool IsActive { get; set; }

    public List<string> Roles { get; set; } = new();
}
