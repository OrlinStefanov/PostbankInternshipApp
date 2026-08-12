using Microsoft.AspNetCore.Components.Authorization;

namespace BinTool.UI.Services;

public interface IAccessTokenProvider
{
    Task<string?> GetTokenAsync();
}

// Deliberately not an HttpClient message handler: those are pooled and resolved from their own
// dependency-injection scope, so a scoped service injected into one is not the instance belonging
// to the current circuit.
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
