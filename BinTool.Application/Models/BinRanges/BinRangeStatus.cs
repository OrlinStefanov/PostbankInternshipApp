namespace BinTool.Application.Models.BinRanges;

/// <summary>
/// Where a BIN range sits relative to today. Derived from the validity dates and the
/// soft-delete flag rather than stored, so it cannot drift out of step with them.
/// </summary>
public enum BinRangeStatus
{
    /// <summary>
    /// Valid today: classification will match it.
    /// </summary>
    Active = 0,

    /// <summary>
    /// Valid from a future date. Stored, but not yet used for classification.
    /// </summary>
    Scheduled = 1,

    /// <summary>
    /// Its <c>ValidTo</c> has passed. Kept for history; no longer classifies.
    /// </summary>
    Expired = 2,

    /// <summary>
    /// Soft-deleted. Excluded from classification, and re-importing the prefix
    /// revives this record rather than creating a second one.
    /// </summary>
    Deleted = 3
}
