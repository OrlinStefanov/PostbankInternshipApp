namespace BinTool.Core.Models.Commission;

/// <summary>
/// A commission rule's lifecycle state, derived rather than stored: it folds the
/// soft-delete flag, the active flag and the validity window into the one label a
/// listing shows. The order matters - a listing sorts live rules ahead of the rest.
/// </summary>
public enum CommissionRuleStatus
{
    /// <summary>Active, not deleted, and valid today.</summary>
    Active = 0,

    /// <summary>Active and not deleted, but its ValidFrom is still in the future.</summary>
    Scheduled = 1,

    /// <summary>Active and not deleted, but its ValidTo has passed.</summary>
    Expired = 2,

    /// <summary>Marked inactive by an admin, so it never takes part in resolution.</summary>
    Inactive = 3,

    /// <summary>Soft-deleted.</summary>
    Deleted = 4
}
