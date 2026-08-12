using System.Text.Json.Serialization;
using BinTool.Application.Models.Common;
namespace BinTool.Application.Models.Access;

public class RoleMutationResult : IMutationResult
{
    public RoleMutationStatus Status { get; set; }

    // The status code says this.
    [JsonIgnore]
    public MutationOutcome Outcome => Status.Outcome();

    public bool Succeeded => Outcome == MutationOutcome.Succeeded;

    public string? Error { get; set; }

    public RoleListItem? Role { get; set; }

    public static RoleMutationResult Success(RoleMutationStatus status, RoleListItem? role = null) =>
        new() { Status = status, Role = role };

    public static RoleMutationResult Failure(RoleMutationStatus status, string error) =>
        new() { Status = status, Error = error };
}
