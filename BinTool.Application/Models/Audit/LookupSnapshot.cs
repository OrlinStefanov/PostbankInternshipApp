namespace BinTool.Application.Models.Audit;

public sealed record LookupSnapshot(string Name, string? Description, bool IsDeleted);
