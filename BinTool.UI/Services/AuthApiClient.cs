using System.Net;
using System.Net.Http.Json;
using BinTool.Core.Models.Auth;

namespace BinTool.UI.Services;

/// <summary>
/// Exchanges credentials for an API token. The only client that runs unauthenticated.
/// </summary>
public class AuthApiClient
{
    private readonly HttpClient _http;

    public AuthApiClient(HttpClient http)
    {
        _http = http;
    }

    /// <summary>
    /// Calls <c>POST /api/Auth/login</c>. Returns null when the credentials are rejected,
    /// which the caller turns into a message - the API deliberately does not say whether
    /// it was the user name or the password that was wrong.
    /// </summary>
    public async Task<LoginResponse?> LoginAsync(
        string userName, string password, CancellationToken cancellationToken = default)
    {
        var response = await _http.PostAsJsonAsync(
            "api/Auth/login", new LoginRequest { UserName = userName, Password = password },
            cancellationToken);

        if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.BadRequest)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<LoginResponse>(cancellationToken);
    }
}
