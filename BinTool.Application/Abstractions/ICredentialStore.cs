using BinTool.Application.Models.Auth;

namespace BinTool.Application.Abstractions;

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
