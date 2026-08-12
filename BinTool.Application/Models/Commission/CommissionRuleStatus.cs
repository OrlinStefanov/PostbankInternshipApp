namespace BinTool.Application.Models.Commission;

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
