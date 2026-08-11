namespace BinTool.Application.Models.Access;

/// <summary>
/// One user as the access-control screen shows them, with the roles they currently hold.
/// No credential fields: this screen assigns roles, it does not manage passwords.
/// </summary>
public class UserListItem
{
    public string Id { get; set; } = string.Empty;

    public string UserName { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;

    public string? Email { get; set; }

    /// <summary>Disabled accounts cannot sign in; shown so an admin knows why a user is dark.</summary>
    public bool IsActive { get; set; }

    public List<string> Roles { get; set; } = new();
}
