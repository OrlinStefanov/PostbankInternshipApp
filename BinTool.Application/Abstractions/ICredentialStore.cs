namespace BinTool.Application.Abstractions;

// The credential operations a sign-in needs, one call each and no policy in any of them: the order
// they run in, and what it means when one of them fails, is settled by SignInService.
public interface ICredentialStore
{
    Task<CredentialUser?> FindByNameOrEmailAsync(
        string userName, CancellationToken cancellationToken = default);

    Task<bool> CheckPasswordAsync(
        string userId, string password, CancellationToken cancellationToken = default);

    Task<DateTimeOffset?> GetLockoutEndAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>Releases any lock and wipes the failed-attempt count.</summary>
    Task ClearLockoutAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>Records a failed attempt, and reports whether it was the one that locked the account.</summary>
    Task<bool> RecordFailedAttemptAsync(string userId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<string>> GetRolesAsync(string userId, CancellationToken cancellationToken = default);

    Task StampLastLoginAsync(
        string userId, DateTime whenUtc, CancellationToken cancellationToken = default);
}

public sealed record CredentialUser(
    string Id,
    string UserName,
    string? Email,
    string? FullName,
    bool IsActive);
