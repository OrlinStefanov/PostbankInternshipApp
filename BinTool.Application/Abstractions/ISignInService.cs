using BinTool.Application.Models.Auth;

namespace BinTool.Application.Abstractions;

public interface ISignInService
{
    /// <summary>Checks credentials and, when they hold up, resolves what the account may do.</summary>
    Task<SignInOutcome> SignInAsync(
        string userName, string password, CancellationToken cancellationToken = default);
}
