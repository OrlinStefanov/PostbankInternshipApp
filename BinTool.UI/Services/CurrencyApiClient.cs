using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using BinTool.Application.Models.Currency;
using BinTool.Application.Models.ReferenceData;
using Microsoft.AspNetCore.Mvc;

namespace BinTool.UI.Services;

public class CurrencyApiClient
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly HttpClient _http;
    private readonly IAccessTokenProvider _tokens;

    public CurrencyApiClient(HttpClient http, IAccessTokenProvider tokens)
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

    public async Task<List<CurrencyListItem>> SearchAsync(
        bool includeDeleted = false, CancellationToken cancellationToken = default)
    {
        await AuthorizeAsync();

        var result = await _http.GetFromJsonAsync<List<CurrencyListItem>>(
            $"api/Currencies?includeDeleted={(includeDeleted ? "true" : "false")}",
            Json, cancellationToken);

        return result ?? new List<CurrencyListItem>();
    }

    public Task<CurrencyMutationResult> CreateAsync(
        CurrencyInput input, CancellationToken cancellationToken = default) =>
        SendAsync(HttpMethod.Post, "api/Currencies", input, cancellationToken);

    public Task<CurrencyMutationResult> UpdateAsync(
        int id, CurrencyInput input, CancellationToken cancellationToken = default) =>
        SendAsync(HttpMethod.Put, $"api/Currencies/{id}", input, cancellationToken);

    public Task<CurrencyMutationResult> DeleteAsync(
        int id, CancellationToken cancellationToken = default) =>
        SendAsync(HttpMethod.Delete, $"api/Currencies/{id}", body: null, cancellationToken);

    public Task<CurrencyMutationResult> RestoreAsync(
        int id, CancellationToken cancellationToken = default) =>
        SendAsync(HttpMethod.Post, $"api/Currencies/{id}/restore", body: null, cancellationToken);

    private async Task<CurrencyMutationResult> SendAsync(
        HttpMethod method, string url, CurrencyInput? body, CancellationToken cancellationToken)
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

        return CurrencyMutationResult.Failure(
            LookupMutationStatus.Invalid, await DescribeAsync(response, cancellationToken));
    }

    private static async Task<CurrencyMutationResult?> ReadResultAsync(
        HttpResponseMessage response, CancellationToken cancellationToken)
    {
        try
        {
            var result = await response.Content
                .ReadFromJsonAsync<CurrencyMutationResult>(Json, cancellationToken);

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
        if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
        {
            return "You are not allowed to change currencies. Sign in as an admin and try again.";
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
