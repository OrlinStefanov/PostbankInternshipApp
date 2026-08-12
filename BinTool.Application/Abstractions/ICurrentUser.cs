namespace BinTool.Application.Abstractions;

public interface ICurrentUser
{
    /// <summary>Identity user id, or null when the caller is not authenticated.</summary>
    string? UserId { get; }

    /// <summary>Name to record on audit fields.</summary>
    string Name { get; }

    /// <summary>
    /// Recorded when work happens outside a request - background seeding, for example.
    /// </summary>
    const string SystemName = "system";
}
