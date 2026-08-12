namespace BinTool.Application.Models.Import;

public class BinFieldDiff
{
    public string Field { get; set; } = string.Empty;

    public string? OldValue { get; set; }

    public string? NewValue { get; set; }
}
