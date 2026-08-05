namespace BinTool.Core.Models.Access;

public enum UserRolesStatus
{
    Updated,

    /// <summary>No user has that id.</summary>
    NotFound,

    /// <summary>The change would leave the system with no admin.</summary>
    LastAdmin,

    /// <summary>The caller tried to remove their own Admin role.</summary>
    SelfDemotion,

    /// <summary>A named role does not exist.</summary>
    UnknownRole,

    Invalid,
}

/// <summary>
/// The outcome of setting a user's roles, in the app's usual result shape.
/// </summary>
public class UserRolesResult
{
    public UserRolesStatus Status { get; set; }

    public bool Succeeded => Status == UserRolesStatus.Updated;

    public string? Error { get; set; }

    /// <summary>The user with their roles as they now stand. Null on a refusal.</summary>
    public UserListItem? User { get; set; }

    public static UserRolesResult Success(UserListItem user) =>
        new() { Status = UserRolesStatus.Updated, User = user };

    public static UserRolesResult Failure(UserRolesStatus status, string error) =>
        new() { Status = status, Error = error };
}
