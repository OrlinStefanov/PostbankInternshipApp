using BinTool.Application.Abstractions;

namespace BinTool.Application.Services;

// The identity used when work happens outside a request - seeding, background jobs, or a test
// exercising a service directly. Audit fields record "system" rather than blank.
public sealed class SystemCurrentUser : ICurrentUser
{
    public string? UserId => null;

    public string Name => ICurrentUser.SystemName;
}
