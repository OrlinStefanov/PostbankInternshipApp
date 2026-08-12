namespace BinTool.Application.Models.BinRanges;

public enum BinRangeStatus
{
    /// <summary>Valid today: classification will match it.</summary>
    Active = 0,

    /// <summary>Valid from a future date.</summary>
    Scheduled = 1,

    /// <summary>Its <c>ValidTo</c> has passed.</summary>
    Expired = 2,

    /// <summary>Soft-deleted.</summary>
    Deleted = 3
}
