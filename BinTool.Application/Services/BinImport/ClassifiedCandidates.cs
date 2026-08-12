using BinTool.Application.Models.Audit;
using BinTool.Application.Models.Import;
using BinTool.Domain.Entities;

namespace BinTool.Application.Services.BinImport;

internal sealed record StagedConflict(
    PendingBinConflict Entity, int RowNumber, List<BinFieldDiff> Diffs, string? Message, string? Advisory);

internal sealed record ClassifiedCandidates(
    List<(BinRange Entity, ResolvedRow Values)> Inserted,
    Dictionary<int, ResolvedRow> Revivals,
    Dictionary<int, BinRangeSnapshot> RevivedFrom,
    List<StagedConflict> StagedConflicts);
