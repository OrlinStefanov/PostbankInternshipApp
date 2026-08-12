namespace BinTool.Application.Models.Access;

public sealed record UserRecord(
    string Id,
    string UserName,
    string FullName,
    string? Email,
    bool IsActive,
    IReadOnlyList<string> Roles);
