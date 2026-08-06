namespace BinTool.Core.Models.ReferenceData;

/// <summary>
/// Derived from a reference row's <c>IsDeleted</c> flag, so a listing communicates the
/// same live-vs-withdrawn distinction the BIN range listing does.
/// </summary>
public enum LookupStatus
{
    /// <summary>The row is available for classification, import and rule resolution.</summary>
    Active = 1,

    /// <summary>The row was soft-deleted and no longer participates in classification.</summary>
    Deleted = 2
}
