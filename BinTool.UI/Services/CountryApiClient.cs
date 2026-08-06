using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using BinTool.Core.Models.ReferenceData;
using Microsoft.AspNetCore.Mvc;

namespace BinTool.UI.Services;

/// <summary>
/// Thin typed client for the country endpoints. Kept separate from
/// <see cref="ReferenceDataApiClient"/> because country carries an ISO code and a
/// region assignment - a different input and output shape.
/// </summary>
public class CountryApiClient
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly HttpClient _http;
    private readonly IAccessTokenProvider _tokens;

    public CountryApiClient(HttpClient http, IAccessTokenProvider tokens)
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

    public async Task<List<CountryListItem>> SearchAsync(
        bool includeDeleted = false, CancellationToken cancellationToken = default)
    {
        await AuthorizeAsync();

        var result = await _http.GetFromJsonAsync<List<CountryListItem>>(
            $"api/Countries?includeDeleted={(includeDeleted ? "true" : "false")}",
            Json, cancellationToken);

        return result ?? new List<CountryListItem>();
    }

    public Task<CountryMutationResult> CreateAsync(
        CountryInput input, CancellationToken cancellationToken = default) =>
        SendAsync(HttpMethod.Post, "api/Countries", input, cancellationToken);

    public Task<CountryMutationResult> UpdateAsync(
        int id, CountryInput input, CancellationToken cancellationToken = default) =>
        SendAsync(HttpMethod.Put, $"api/Countries/{id}", input, cancellationToken);

    public Task<CountryMutationResult> DeleteAsync(
        int id, CancellationToken cancellationToken = default) =>
        SendAsync(HttpMethod.Delete, $"api/Countries/{id}", body: null, cancellationToken);

    public Task<CountryMutationResult> RestoreAsync(
        int id, CancellationToken cancellationToken = default) =>
        SendAsync(HttpMethod.Post, $"api/Countries/{id}/restore", body: null, cancellationToken);

    private async Task<CountryMutationResult> SendAsync(
        HttpMethod method, string url, CountryInput? body, CancellationToken cancellationToken)
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

        return CountryMutationResult.Failure(
            LookupMutationStatus.Invalid, await DescribeAsync(response, cancellationToken));
    }

    private static async Task<CountryMutationResult?> ReadResultAsync(
        HttpResponseMessage response, CancellationToken cancellationToken)
    {
        try
        {
            var result = await response.Content
                .ReadFromJsonAsync<CountryMutationResult>(Json, cancellationToken);

            return result is null || (!result.Succeeded && result.Error is null) ? null : result;
        }
        catch (Exception ex) when (ex is JsonException or NotSupportedException)
        {
            return null;
        }
    }

    private static async Task<string> DescribeAsync(
        HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.StatusCode is System.Net.HttpStatusCode.Unauthorized
                                or System.Net.HttpStatusCode.Forbidden)
        {
            return "You are not allowed to change countries. Sign in as an admin and try again.";
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
            // Fall through to the status line.
        }

        return $"The request failed ({(int)response.StatusCode} {response.ReasonPhrase}).";
    }
}
