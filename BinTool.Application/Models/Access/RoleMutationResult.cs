using System.Text.Json.Serialization;
using BinTool.Application.Models.Common;
namespace BinTool.Application.Models.Access;

public class RoleMutationResult : IMutationResult
{
    public RoleMutationStatus Status { get; set; }

    // The status code carries this, so it is not repeated in the body.
    [JsonIgnore]
    public MutationOutcome Outcome => Status.Outcome();

    public bool Succeeded => Outcome == MutationOutcome.Succeeded;

    /// <summary>Why the write was refused.</summary>
    public string? Error { get; set; }

    /// <summary>The role as it now stands.</summary>
    public RoleListItem? Role { get; set; }

    public static RoleMutationResult Success(RoleMutationStatus status, RoleListItem? role = null) =>
        new() { Status = status, Role = role };

    public static RoleMutationResult Failure(RoleMutationStatus status, string error) =>
        new() { Status = status, Error = error };
}
