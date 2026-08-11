using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using BinTool.Application.Authorization;
using BinTool.Application.Models.Access;
using Microsoft.AspNetCore.Mvc;

namespace BinTool.UI.Services;

/// <summary>
/// Typed client over the API's roles and users endpoints. Runs on the Blazor server, so calls
/// are server-to-server. Writes do not throw on a refusal - the API answers 400/404/409 with the
/// same result body, and the caller reads its <c>Error</c>.
/// </summary>
public class AccessControlApiClient
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly HttpClient _http;
    private readonly IAccessTokenProvider _tokens;

    public AccessControlApiClient(HttpClient http, IAccessTokenProvider tokens)
    {
        _http = http;
        _tokens = tokens;
    }

    private async Task AuthorizeAsync()
    {
        var token = await _tokens.GetTokenAsync();

        _http.DefaultRequestHeaders.Authorization = token is null
            ? null
            : new AuthenticationHeaderValue("Bearer", token);
    }

    // ---- Reads -----------------------------------------------------------------

    public async Task<List<RoleListItem>> GetRolesAsync(CancellationToken cancellationToken = default)
    {
        await AuthorizeAsync();

        return await _http.GetFromJsonAsync<List<RoleListItem>>(
            "api/Roles", Json, cancellationToken) ?? new();
    }

    public async Task<List<PermissionInfo>> GetPermissionsAsync(CancellationToken cancellationToken = default)
    {
        await AuthorizeAsync();

        return await _http.GetFromJsonAsync<List<PermissionInfo>>(
            "api/Roles/permissions", Json, cancellationToken) ?? new();
    }

    public async Task<List<UserListItem>> GetUsersAsync(CancellationToken cancellationToken = default)
    {
        await AuthorizeAsync();

        return await _http.GetFromJsonAsync<List<UserListItem>>(
            "api/Users", Json, cancellationToken) ?? new();
    }

    // ---- Writes ----------------------------------------------------------------

    public Task<RoleMutationResult> CreateRoleAsync(
        RoleInput input, CancellationToken cancellationToken = default) =>
        SendAsync(HttpMethod.Post, "api/Roles", input,
            RoleMutationResult.Failure, cancellationToken);

    public Task<RoleMutationResult> UpdateRoleAsync(
        string roleId, RoleInput input, CancellationToken cancellationToken = default) =>
        SendAsync(HttpMethod.Put, $"api/Roles/{roleId}", input,
            RoleMutationResult.Failure, cancellationToken);

    public Task<RoleMutationResult> DeleteRoleAsync(
        string roleId, CancellationToken cancellationToken = default) =>
        SendAsync(HttpMethod.Delete, $"api/Roles/{roleId}", null,
            RoleMutationResult.Failure, cancellationToken);

    public Task<UserRolesResult> SetUserRolesAsync(
        string userId, UserRolesInput input, CancellationToken cancellationToken = default) =>
        SendAsync(HttpMethod.Put, $"api/Users/{userId}/roles", input,
            UserRolesResult.Failure, cancellationToken);

    // ---- Shared send -----------------------------------------------------------

    private Task<RoleMutationResult> SendAsync(
        HttpMethod method, string url, object? body,
        Func<RoleMutationStatus, string, RoleMutationResult> failure,
        CancellationToken cancellationToken) =>
        SendAsync(method, url, body,
            r => r.Succeeded || r.Error is not null,
            error => failure(RoleMutationStatus.Invalid, error), cancellationToken);

    private Task<UserRolesResult> SendAsync(
        HttpMethod method, string url, object? body,
        Func<UserRolesStatus, string, UserRolesResult> failure,
        CancellationToken cancellationToken) =>
        SendAsync(method, url, body,
            r => r.Succeeded || r.Error is not null,
            error => failure(UserRolesStatus.Invalid, error), cancellationToken);

    /// <summary>
    /// Runs one write and reads the result. A refusal the API produces (its own body with a
    /// 400/404/409) is read as-is; anything else - a validation problem raised before the action
    /// ran, an unauthorized call, a proxy error page - is turned into a failure the UI can show.
    /// </summary>
    private async Task<TResult> SendAsync<TResult>(
        HttpMethod method, string url, object? body,
        Func<TResult, bool> looksLikeResult, Func<string, TResult> onOther,
        CancellationToken cancellationToken) where TResult : class
    {
        await AuthorizeAsync();

        using var request = new HttpRequestMessage(method, url);
        if (body is not null)
        {
            request.Content = JsonContent.Create(body, body.GetType(), options: Json);
        }

        using var response = await _http.SendAsync(request, cancellationToken);

        try
        {
            var parsed = await response.Content.ReadFromJsonAsync<TResult>(Json, cancellationToken);
            if (parsed is not null && looksLikeResult(parsed))
            {
                return parsed;
            }
        }
        catch (Exception ex) when (ex is JsonException or NotSupportedException)
        {
            // Fall through to the descriptive failure.
        }

        return onOther(await DescribeAsync(response, cancellationToken));
    }

    /// <summary>Turns a response the client did not expect into one sentence a user can act on.</summary>
    private static async Task<string> DescribeAsync(
        HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
        {
            return "You are not allowed to manage access. Sign in as an admin and try again.";
        }

        try
        {
            var problem = await response.Content
                .ReadFromJsonAsync<ValidationProblemDetails>(Json, cancellationToken);

            var messages = problem?.Errors.SelectMany(e => e.Value).ToArray();
            if (messages is { Length: > 0 }) return string.Join(" ", messages);

            if (!string.IsNullOrWhiteSpace(problem?.Title)) return problem!.Title!;
        }
        catch (Exception ex) when (ex is JsonException or NotSupportedException)
        {
            // Fall through to the status code.
        }

        return $"The request failed ({(int)response.StatusCode} {response.ReasonPhrase}).";
    }
}
