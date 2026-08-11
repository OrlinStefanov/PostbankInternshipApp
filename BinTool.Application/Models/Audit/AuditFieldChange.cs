namespace BinTool.Application.Models.Audit;

/// <summary>
/// How one field fared between the before and after snapshots of an audit entry.
/// </summary>
public enum AuditChangeKind
{
    /// <summary>
    /// Present in both snapshots with the same value. Carried so a reader can ask for the
    /// full row, not only the parts that moved.
    /// </summary>
    Unchanged,

    /// <summary>
    /// Absent before, present after - every field of a Created entry looks like this.
    /// </summary>
    Added,

    /// <summary>
    /// Present before, absent after - every field of a hard Deleted entry looks like this.
    /// </summary>
    Removed,

    /// <summary>
    /// Present in both, with different values.
    /// </summary>
    Changed
}

/// <summary>
/// One field lined up across an audit entry's two snapshots.
/// <para>
/// <see cref="OldValue"/> and <see cref="NewValue"/> are the JSON values rendered as text:
/// a string keeps its contents, a number or boolean its literal form, and a nested object
/// or array its compact JSON. A null means the field was JSON <c>null</c> or was absent on
/// that side - which of the two is told by <see cref="Kind"/>.
/// </para>
/// </summary>
public sealed class AuditFieldChange
{
    /// <summary>
    /// Property name exactly as the snapshot recorded it. Deliberately not prettified:
    /// it is the name of the entity property the change was written against, and an audit
    /// reader is better served by the real name than by a nicer-looking one.
    /// </summary>
    public string Field { get; init; } = string.Empty;

    public string? OldValue { get; init; }

    public string? NewValue { get; init; }

    public AuditChangeKind Kind { get; init; }
}
