using BinTool.Core.Services;

namespace BinTool.Infrastructure.Services;

/// <summary>
/// The identity used when work happens outside a request - seeding, background jobs, or
/// a test exercising a service directly. Audit fields record "system" rather than blank.
/// </summary>
public sealed class SystemCurrentUser : ICurrentUser
{
    public string? UserId => null;

    public string Name => ICurrentUser.SystemName;
}
