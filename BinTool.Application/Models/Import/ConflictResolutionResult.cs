namespace BinTool.Application.Models.Import;

public class ConflictResolutionResult
{
    public int UpdatedCount { get; set; }

    public int DiscardedCount { get; set; }

    public int NotFoundCount { get; set; }
}
