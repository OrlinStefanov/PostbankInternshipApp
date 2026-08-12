namespace BinTool.Application.Services.BinImport;

internal sealed record ImportCandidate(int RowNumber, string Raw, ResolvedRow Values);
