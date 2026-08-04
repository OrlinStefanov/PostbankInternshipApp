using Microsoft.AspNetCore.Components.Authorization;

namespace BinTool.UI.Services;

public interface IAccessTokenProvider
{
    /// <summary>
    /// The signed-in user's API token, or null when nobody is signed in.
    /// </summary>
    Task<string?> GetTokenAsync();
}

/// <summary>
/// Reads the API token out of the authenticated principal.
/// <para>
/// Deliberately not an <c>HttpClient</c> message handler: those are pooled and resolved
/// from their own dependency-injection scope, so a scoped service injected into one is
/// not the instance belonging to the current circuit. Asking the authentication state
/// per call keeps the token tied to the user actually signed in.
/// </para>
/// </summary>
public class AccessTokenProvider : IAccessTokenProvider
{
    private readonly AuthenticationStateProvider _authentication;

    public AccessTokenProvider(AuthenticationStateProvider authentication)
    {
        _authentication = authentication;
    }

    public async Task<string?> GetTokenAsync()
    {
        var state = await _authentication.GetAuthenticationStateAsync();

        return state.User.Identity?.IsAuthenticated == true
            ? state.User.FindFirst(AuthClaims.AccessToken)?.Value
            : null;
    }
}
