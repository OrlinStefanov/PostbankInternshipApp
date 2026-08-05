namespace BinTool.Core.Models.Access;

/// <summary>
/// One role as it appears in the access-control listing: what it is, how many people hold it,
/// and which permissions it grants.
/// </summary>
public class RoleListItem
{
    public string Id { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    /// <summary>
    /// True for the built-in Admin role. A protected role cannot be renamed, deleted or have
    /// its permissions changed, and it holds every permission implicitly - the client uses this
    /// to disable its edit and delete controls.
    /// </summary>
    public bool IsProtected { get; set; }

    /// <summary>How many users currently hold the role.</summary>
    public int MemberCount { get; set; }

    /// <summary>
    /// The permission keys the role grants. Empty for the protected Admin role, which is a
    /// superuser and is not driven by explicit grants.
    /// </summary>
    public List<string> Permissions { get; set; } = new();
}
