namespace BinTool.Application.Models.Access;

public sealed record RoleRecord(
    string Id,
    string Name,
    string? Description,
    IReadOnlyList<string> Permissions,
    int MemberCount);
