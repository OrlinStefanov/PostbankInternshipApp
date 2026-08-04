namespace BinTool.Core.Models.Import;

/// <summary>
/// Outcome of resolving a batch of staged conflicts.
/// </summary>
public class ConflictResolutionResult
{
    /// <summary>
    /// Conflicts whose values were written onto the existing BIN range.
    /// </summary>
    public int UpdatedCount { get; set; }

    /// <summary>
    /// Conflicts the user chose to discard, leaving the existing record intact.
    /// </summary>
    public int DiscardedCount { get; set; }

    /// <summary>
    /// Requested ids that were not found, or were already resolved.
    /// </summary>
    public int NotFoundCount { get; set; }
}
