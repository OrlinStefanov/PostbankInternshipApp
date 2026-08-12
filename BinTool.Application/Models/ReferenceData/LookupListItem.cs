namespace BinTool.Application.Models.ReferenceData;

public class LookupListItem
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    /// <summary>Derived from the soft-delete flag, not stored.</summary>
    public LookupStatus Status { get; set; }

    public DateTime? DeletedAt { get; set; }

    public string? DeletedBy { get; set; }
}
