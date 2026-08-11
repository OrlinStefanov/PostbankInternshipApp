namespace BinTool.Application.Models.Import;

/// <summary>
/// A single field that differs between an incoming CSV row and the existing
/// database record with the same prefix. Values are formatted for display.
/// </summary>
public class BinFieldDiff
{
    public string Field { get; set; } = string.Empty;

    /// <summary>
    /// Current value in the database.
    /// </summary>
    public string? OldValue { get; set; }

    /// <summary>
    /// Proposed value from the imported file.
    /// </summary>
    public string? NewValue { get; set; }
}
