namespace BinTool.Application.Models.Import;

public class BinFieldDiff
{
    public string Field { get; set; } = string.Empty;

    /// <summary>Current value in the database.</summary>
    public string? OldValue { get; set; }

    /// <summary>Proposed value from the imported file.</summary>
    public string? NewValue { get; set; }
}
