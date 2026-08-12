namespace BinTool.Application.Models.Audit;

public enum AuditChangeKind
{
    /// <summary>Present in both snapshots with the same value.</summary>
    Unchanged,

    /// <summary>
    /// Absent before, present after - every field of a Created entry looks like this.
    /// </summary>
    Added,

    /// <summary>
    /// Present before, absent after - every field of a hard Deleted entry looks like this.
    /// </summary>
    Removed,

    /// <summary>Present in both, with different values.</summary>
    Changed
}

public sealed class AuditFieldChange
{
    /// <summary>Property name exactly as the snapshot recorded it.</summary>
    public string Field { get; init; } = string.Empty;

    public string? OldValue { get; init; }

    public string? NewValue { get; init; }

    public AuditChangeKind Kind { get; init; }
}
