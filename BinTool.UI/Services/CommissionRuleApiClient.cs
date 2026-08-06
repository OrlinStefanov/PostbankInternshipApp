using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using BinTool.Core.Models.Commission;
using Microsoft.AspNetCore.Mvc;

namespace BinTool.UI.Services;

/// <summary>
/// Thin typed client over the BinTool API's commission-rule endpoints. Runs on the Blazor
/// server, so calls are server-to-server (no CORS involved).
/// </summary>
public class CommissionRuleApiClient
{
    /// <summary>
    /// The API writes enums as their names, which the default options will not read back.
    /// </summary>
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly HttpClient _http;
    private readonly IAccessTokenProvider _tokens;

    public CommissionRuleApiClient(HttpClient http, IAccessTokenProvider tokens)
    {
        _http = http;
        _tokens = tokens;
    }

    /// <summary>
    /// Attaches the signed-in user's token. Safe to set on the instance: a typed client
    /// gets its own <c>HttpClient</c>, so this never leaks across users.
    /// </summary>
    private async Task AuthorizeAsync()
    {
        var token = await _tokens.GetTokenAsync();

        _http.DefaultRequestHeaders.Authorization = token is null
            ? null
            : new AuthenticationHeaderValue("Bearer", token);
    }

    /// <summary>
    /// Lists commission rules from <c>GET /api/CommissionRules</c>. Expired rules are hidden
    /// unless <paramref name="includeExpired"/> is set; deleted ones unless
    /// <paramref name="includeDeleted"/> is set.
    /// </summary>
    public async Task<List<CommissionRuleListItem>> SearchAsync(
        bool includeDeleted = false,
        bool includeExpired = false,
        CancellationToken cancellationToken = default)
    {
        await AuthorizeAsync();

        var url = $"api/CommissionRules?includeDeleted={(includeDeleted ? "true" : "false")}" +
                  $"&includeExpired={(includeExpired ? "true" : "false")}";

        var result = await _http.GetFromJsonAsync<List<CommissionRuleListItem>>(
            url, Json, cancellationToken);

        return result ?? new List<CommissionRuleListItem>();
    }

    /// <summary>
    /// Fetches one rule by id from <c>GET /api/CommissionRules/{id}</c>, deleted ones
    /// included. Returns null if no rule has that id.
    /// </summary>
    public async Task<CommissionRuleListItem?> GetAsync(
        int id, CancellationToken cancellationToken = default)
    {
        await AuthorizeAsync();

        using var response = await _http.GetAsync($"api/CommissionRules/{id}", cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound) return null;

        response.EnsureSuccessStatusCode();

        return await response.Content
            .ReadFromJsonAsync<CommissionRuleListItem>(Json, cancellationToken);
    }

    /// <summary>Adds a rule via <c>POST /api/CommissionRules</c>.</summary>
    public Task<CommissionRuleMutationResult> CreateAsync(
        CommissionRuleInput input, CancellationToken cancellationToken = default) =>
        SendAsync(HttpMethod.Post, "api/CommissionRules", input, cancellationToken);

    /// <summary>Overwrites a rule via <c>PUT /api/CommissionRules/{id}</c>.</summary>
    public Task<CommissionRuleMutationResult> UpdateAsync(
        int id, CommissionRuleInput input, CancellationToken cancellationToken = default) =>
        SendAsync(HttpMethod.Put, $"api/CommissionRules/{id}", input, cancellationToken);

    /// <summary>Soft-deletes a rule via <c>DELETE /api/CommissionRules/{id}</c>.</summary>
    public Task<CommissionRuleMutationResult> DeleteAsync(
        int id, CancellationToken cancellationToken = default) =>
        SendAsync(HttpMethod.Delete, $"api/CommissionRules/{id}", null, cancellationToken);

    /// <summary>Restores a soft-deleted rule via <c>POST /api/CommissionRules/{id}/restore</c>.</summary>
    public Task<CommissionRuleMutationResult> RestoreAsync(
        int id, CancellationToken cancellationToken = default) =>
        SendAsync(HttpMethod.Post, $"api/CommissionRules/{id}/restore", null, cancellationToken);

    /// <summary>Makes a rule the fallback default via <c>POST /api/CommissionRules/{id}/default</c>.</summary>
    public Task<CommissionRuleMutationResult> SetDefaultAsync(
        int id, CancellationToken cancellationToken = default) =>
        SendAsync(HttpMethod.Post, $"api/CommissionRules/{id}/default", null, cancellationToken);

    /// <summary>Clears the configured default via <c>DELETE /api/CommissionRules/default</c>.</summary>
    public Task<CommissionRuleMutationResult> ClearDefaultAsync(
        CancellationToken cancellationToken = default) =>
        SendAsync(HttpMethod.Delete, "api/CommissionRules/default", null, cancellationToken);

    /// <summary>
    /// Runs one write and reads the outcome. A refusal is a normal answer here - the API
    /// returns the same body with a 400, 404 or 409 - so the status code is not thrown on;
    /// the caller reads <c>Status</c> and shows <c>Error</c>.
    /// </summary>
    private async Task<CommissionRuleMutationResult> SendAsync(
        HttpMethod method, string url, CommissionRuleInput? body, CancellationToken cancellationToken)
    {
        await AuthorizeAsync();

        using var request = new HttpRequestMessage(method, url);
        if (body is not null)
        {
            request.Content = JsonContent.Create(body, options: Json);
        }

        using var response = await _http.SendAsync(request, cancellationToken);

        var result = await ReadResultAsync(response, cancellationToken);
        if (result is not null) return result;

        // Anything that is not the API's own result shape - a validation problem raised
        // before the action ran, an unauthorized call, a proxy error page.
        return CommissionRuleMutationResult.Failure(
            CommissionRuleMutationStatus.Invalid, await DescribeAsync(response, cancellationToken));
    }

    private static async Task<CommissionRuleMutationResult?> ReadResultAsync(
        HttpResponseMessage response, CancellationToken cancellationToken)
    {
        try
        {
            var result = await response.Content
                .ReadFromJsonAsync<CommissionRuleMutationResult>(Json, cancellationToken);

            // A body that parses but says nothing is not the result shape - every refusal
            // the API produces carries a reason.
            return result is null || (!result.Succeeded && result.Error is null) ? null : result;
        }
        catch (Exception ex) when (ex is JsonException or NotSupportedException)
        {
            return null;
        }
    }

    /// <summary>
    /// Turns a response the client did not expect into one sentence a user can act on.
    /// </summary>
    private static async Task<string> DescribeAsync(
        HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
        {
            return "You are not allowed to change commission rules. Sign in as an admin and try again.";
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
