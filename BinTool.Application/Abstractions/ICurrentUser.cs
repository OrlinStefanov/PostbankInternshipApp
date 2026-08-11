namespace BinTool.Application.Abstractions;

/// <summary>
/// The identity behind the request being handled, for stamping audit fields without
/// giving the domain services a dependency on ASP.NET's HttpContext.
/// </summary>
public interface ICurrentUser
{
    /// <summary>
    /// Identity user id, or null when the caller is not authenticated.
    /// </summary>
    string? UserId { get; }

    /// <summary>
    /// Name to record on audit fields. Falls back to <see cref="SystemName"/> when there is
    /// no authenticated caller, so an audit trail is never left blank.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Recorded when work happens outside a request - background seeding, for example.
    /// </summary>
    const string SystemName = "system";
}
