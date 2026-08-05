namespace BinTool.Core.Models.Access;

/// <summary>
/// The outcome of a role write, in the same shape used across the app: a status, the reason on
/// a refusal, and the role as it now stands on success.
/// </summary>
public class RoleMutationResult
{
    public RoleMutationStatus Status { get; set; }

    public bool Succeeded =>
        Status is RoleMutationStatus.Created
            or RoleMutationStatus.Updated
            or RoleMutationStatus.Deleted;

    /// <summary>Why the write was refused. Null when it succeeded.</summary>
    public string? Error { get; set; }

    /// <summary>The role as it now stands. Null on a refusal, and on a successful delete.</summary>
    public RoleListItem? Role { get; set; }

    public static RoleMutationResult Success(RoleMutationStatus status, RoleListItem? role = null) =>
        new() { Status = status, Role = role };

    public static RoleMutationResult Failure(RoleMutationStatus status, string error) =>
        new() { Status = status, Error = error };
}
