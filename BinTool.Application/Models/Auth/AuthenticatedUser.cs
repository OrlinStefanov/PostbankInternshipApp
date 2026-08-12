namespace BinTool.Application.Models.Auth;

public sealed record AuthenticatedUser(
    string Id,
    string UserName,
    string? Email,
    string? FullName,
    IReadOnlyList<string> Roles,
    IReadOnlyList<string> Permissions);
