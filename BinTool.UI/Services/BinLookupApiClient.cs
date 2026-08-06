using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using BinTool.Core.Models.Classification;
using Microsoft.AspNetCore.Mvc;

namespace BinTool.UI.Services;

/// <summary>
/// Thin typed client over the BinTool API's classification endpoint. Runs on the Blazor
/// server, so calls are server-to-server (no CORS involved).
/// </summary>
public class BinLookupApiClient
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

    public BinLookupApiClient(HttpClient http, IAccessTokenProvider tokens)
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
    /// Classifies a BIN via <c>POST /api/Bin/classify</c>, optionally pricing an amount. A
    /// "no match" is a normal 200 answer with <c>Matched = false</c>; only a rejected input
    /// (400) or a transport failure throws, with a message the page can show.
    /// </summary>
    public async Task<BinClassificationResult> ClassifyAsync(
        BinClassificationRequest request, CancellationToken cancellationToken = default)
    {
        await AuthorizeAsync();

        using var response = await _http.PostAsJsonAsync(
            "api/Bin/classify", request, Json, cancellationToken);

        if (response.IsSuccessStatusCode)
        {
            var result = await response.Content
                .ReadFromJsonAsync<BinClassificationResult>(Json, cancellationToken);

            return result ?? new BinClassificationResult { Bin = request.Bin };
        }

        throw new InvalidOperationException(await DescribeAsync(response, cancellationToken));
    }

    /// <summary>
    /// Turns a response the client did not expect into one sentence a user can act on.
    /// </summary>
    private static async Task<string> DescribeAsync(
        HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.StatusCode is System.Net.HttpStatusCode.Unauthorized
                                or System.Net.HttpStatusCode.Forbidden)
        {
            return "You are not allowed to classify BINs. Sign in and try again.";
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

        return $"The lookup failed ({(int)response.StatusCode} {response.ReasonPhrase}).";
    }
}
