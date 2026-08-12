namespace BinTool.Application.Models.Auth;

public sealed record CredentialUser(
    string Id,
    string UserName,
    string? Email,
    string? FullName,
    bool IsActive);
