using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using BinTool.Application.Models.ReferenceData;
using Microsoft.AspNetCore.Mvc;

namespace BinTool.UI.Services;

/// <summary>
/// Thin typed client for the four Name+Description reference endpoints. One kind is
/// mapped to one route segment on each call, so the caller works in <see cref="LookupKind"/>
/// rather than in strings.
/// </summary>
public class ReferenceDataApiClient
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly HttpClient _http;
    private readonly IAccessTokenProvider _tokens;

    public ReferenceDataApiClient(HttpClient http, IAccessTokenProvider tokens)
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

    public async Task<List<LookupListItem>> SearchAsync(
        LookupKind kind, bool includeDeleted = false, CancellationToken cancellationToken = default)
    {
        await AuthorizeAsync();

        var result = await _http.GetFromJsonAsync<List<LookupListItem>>(
            $"api/{Route(kind)}?includeDeleted={(includeDeleted ? "true" : "false")}",
            Json, cancellationToken);

        return result ?? new List<LookupListItem>();
    }

    public Task<LookupMutationResult> CreateAsync(
        LookupKind kind, LookupInput input, CancellationToken cancellationToken = default) =>
        SendAsync(HttpMethod.Post, $"api/{Route(kind)}", input, cancellationToken);

    public Task<LookupMutationResult> UpdateAsync(
        LookupKind kind, int id, LookupInput input, CancellationToken cancellationToken = default) =>
        SendAsync(HttpMethod.Put, $"api/{Route(kind)}/{id}", input, cancellationToken);

    public Task<LookupMutationResult> DeleteAsync(
        LookupKind kind, int id, CancellationToken cancellationToken = default) =>
        SendAsync(HttpMethod.Delete, $"api/{Route(kind)}/{id}", body: null, cancellationToken);

    public Task<LookupMutationResult> RestoreAsync(
        LookupKind kind, int id, CancellationToken cancellationToken = default) =>
        SendAsync(HttpMethod.Post, $"api/{Route(kind)}/{id}/restore", body: null, cancellationToken);

    /// <summary>
    /// One write, one read. Refusals arrive with a mutation-result body and the caller
    /// reads <c>Status</c> and shows <c>Error</c>; only a transport failure throws.
    /// </summary>
    private async Task<LookupMutationResult> SendAsync(
        HttpMethod method, string url, LookupInput? body, CancellationToken cancellationToken)
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

        return LookupMutationResult.Failure(
            LookupMutationStatus.Invalid, await DescribeAsync(response, cancellationToken));
    }

    private static async Task<LookupMutationResult?> ReadResultAsync(
        HttpResponseMessage response, CancellationToken cancellationToken)
    {
        try
        {
            var result = await response.Content
                .ReadFromJsonAsync<LookupMutationResult>(Json, cancellationToken);

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
            return "You are not allowed to change reference data. Sign in as an admin and try again.";
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

    /// <summary>
    /// The route segment on the API matches the controller name (Pascal-plural).
    /// </summary>
    public static string Route(LookupKind kind) => kind switch
    {
        LookupKind.CardScheme => "CardSchemes",
        LookupKind.ProductType => "ProductTypes",
        LookupKind.FundingType => "FundingTypes",
        LookupKind.Region => "Regions",
        _ => throw new ArgumentOutOfRangeException(nameof(kind))
    };
}
