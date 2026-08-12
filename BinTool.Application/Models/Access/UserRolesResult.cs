using System.Text.Json.Serialization;
using BinTool.Application.Models.Common;
namespace BinTool.Application.Models.Access;

public class UserRolesResult : IMutationResult
{
    public UserRolesStatus Status { get; set; }

    // The status code says this.
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

    public UserListItem? User { get; set; }

    public static UserRolesResult Success(UserListItem user) =>
        new() { Status = UserRolesStatus.Updated, User = user };

    public static UserRolesResult Failure(UserRolesStatus status, string error) =>
        new() { Status = status, Error = error };
}
