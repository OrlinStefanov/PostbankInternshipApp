namespace BinTool.Core.Models.Access;

/// <summary>
/// The outcome of creating, editing or deleting a role. A refusal is a status, not an
/// exception - the caller shows the reason.
/// </summary>
public enum RoleMutationStatus
{
    Created,
    Updated,
    Deleted,

    /// <summary>No role has that id.</summary>
    NotFound,

    /// <summary>Another role already owns that name.</summary>
    NameInUse,

    /// <summary>The Admin role is protected: it cannot be renamed, deleted or have its
    /// permissions changed.</summary>
    Protected,

    /// <summary>The role still has members, so it cannot be deleted.</summary>
    InUse,

    /// <summary>A field broke a rule - a blank name, or an unknown permission key.</summary>
    Invalid,
}
