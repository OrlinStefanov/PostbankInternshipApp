using BinTool.Application.Models.Import;
using BinTool.Domain.Entities;

namespace BinTool.Application.Services.BinImport;

internal static class BinConflictMapper
{
    public static BinConflict FromStaged(StagedConflict s, string? detectedName) => new()
    {
        PendingBinConflictId = s.Entity.PendingBinConflictId,
        RowNumber = s.RowNumber,
        Prefix = s.Entity.Prefix,
        ConflictType = s.Entity.ConflictType.ToString(),
        Message = s.Message,
        Differences = s.Diffs,
        DetectedScheme = detectedName,
        SchemeAdvisory = s.Advisory
    };

    public static BinConflict ForPending(
        PendingBinConflict conflict, List<BinFieldDiff> diffs,
        string? detectedName, string? advisory, string? message = null) => new()
    {
        PendingBinConflictId = conflict.PendingBinConflictId,
        RowNumber = 0,
        Prefix = conflict.Prefix,
        ConflictType = conflict.ConflictType.ToString(),
        Message = message,
        Differences = diffs,
        DetectedScheme = detectedName,
        SchemeAdvisory = advisory
    };
}
