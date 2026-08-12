namespace BinTool.Application.Models.ReferenceData;

public enum LookupStatus
{
    /// <summary>The row is available for classification, import and rule resolution.</summary>
    Active = 1,

    /// <summary>The row was soft-deleted and no longer participates in classification.</summary>
    Deleted = 2
}
