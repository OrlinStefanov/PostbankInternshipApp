using System.Text.Json.Serialization;
using BinTool.Application.Models.Common;
namespace BinTool.Application.Models.Access;

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
public class UserRolesResult : IMutationResult
{
    public UserRolesStatus Status { get; set; }

    // The status code carries this, so it is not repeated in the body.
    [JsonIgnore]
    public MutationOutcome Outcome => Status switch
    {
        UserRolesStatus.NotFound => MutationOutcome.NotFound,
        UserRolesStatus.Invalid => MutationOutcome.Invalid,
        UserRolesStatus.UnknownRole => MutationOutcome.Invalid,
        UserRolesStatus.LastAdmin => MutationOutcome.Conflict,
        UserRolesStatus.SelfDemotion => MutationOutcome.Conflict,
        _ => MutationOutcome.Succeeded
    };

    public bool Succeeded => Outcome == MutationOutcome.Succeeded;

    public string? Error { get; set; }

    /// <summary>The user with their roles as they now stand. Null on a refusal.</summary>
    public UserListItem? User { get; set; }

    public static UserRolesResult Success(UserListItem user) =>
        new() { Status = UserRolesStatus.Updated, User = user };

    public static UserRolesResult Failure(UserRolesStatus status, string error) =>
        new() { Status = status, Error = error };
}
