using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using BinTool.Application.Models.Auth;

namespace BinTool.UI.Services;

public record LoginOutcome(LoginResponse? Response, LoginRejection? Rejection)
{
    public bool Succeeded => Response is not null;
}

public class AuthApiClient
{
    // The API writes enums as their names (JsonStringEnumConverter), so LoginRejection.Reason
    // arrives as "LockedOut", not a number - match that when reading it back.
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly HttpClient _http;

    public AuthApiClient(HttpClient http)
    {
        _http = http;
    }

    // Calls "POST /api/Auth/login". On success carries the token; on a 401 carries the
    // LoginRejection so the caller can tell a lockout apart from a wrong password. The API still
    // does not say whether it was the user name or the password that was wrong.
    public async Task<LoginOutcome> LoginAsync(
        string userName, string password, CancellationToken cancellationToken = default)
    {
        var response = await _http.PostAsJsonAsync(
            "api/Auth/login", new LoginRequest { UserName = userName, Password = password },
            cancellationToken);

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            // The body is a LoginRejection; fall back to a plain invalid-credentials reason
            // if for any reason it cannot be read.
            var rejection =
                await response.Content.ReadFromJsonAsync<LoginRejection>(JsonOptions, cancellationToken)
                ?? new LoginRejection { Reason = LoginRejectionReason.InvalidCredentials };

            return new LoginOutcome(null, rejection);
        }

        if (response.StatusCode == HttpStatusCode.BadRequest)
        {
            return new LoginOutcome(null, new LoginRejection { Reason = LoginRejectionReason.InvalidCredentials });
        }

        response.EnsureSuccessStatusCode();

        var login = await response.Content.ReadFromJsonAsync<LoginResponse>(cancellationToken);
        return new LoginOutcome(login, null);
    }
}
