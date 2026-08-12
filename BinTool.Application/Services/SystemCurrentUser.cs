using BinTool.Application.Abstractions;

namespace BinTool.Application.Services;

public sealed class SystemCurrentUser : ICurrentUser
{
    public string? UserId => null;

    public string Name => ICurrentUser.SystemName;
}
