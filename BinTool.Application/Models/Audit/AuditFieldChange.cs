namespace BinTool.Application.Models.Audit;

public sealed class AuditFieldChange
{
    public string Field { get; init; } = string.Empty;

    public string? OldValue { get; init; }

    public string? NewValue { get; init; }

    public AuditChangeKind Kind { get; init; }
}
