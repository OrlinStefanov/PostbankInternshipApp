using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using BinTool.Core.Models.BinRanges;
using Microsoft.AspNetCore.Mvc;

namespace BinTool.UI.Services;

/// <summary>
/// Thin typed client over the BinTool API's BIN range browse endpoints. Runs on the
/// Blazor server, so calls are server-to-server (no CORS involved).
/// </summary>
public class BinRangeApiClient
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

    public BinRangeApiClient(HttpClient http, IAccessTokenProvider tokens)
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
    /// Fetches one page of BIN ranges from <c>GET /api/BinRanges</c>.
    /// </summary>
    public async Task<PagedResult<BinRangeListItem>> SearchAsync(
        BinRangeQuery query, CancellationToken cancellationToken = default)
    {
        await AuthorizeAsync();

        var result = await _http.GetFromJsonAsync<PagedResult<BinRangeListItem>>(
            $"api/BinRanges?{BuildQueryString(query)}", Json, cancellationToken);

        return result ?? new PagedResult<BinRangeListItem>();
    }

    /// <summary>
    /// Fetches one BIN range by id from <c>GET /api/BinRanges/{id}</c>, deleted ones
    /// included. Returns null if no range has that id.
    /// </summary>
    public async Task<BinRangeListItem?> GetAsync(
        int binRangeId, CancellationToken cancellationToken = default)
    {
        await AuthorizeAsync();

        using var response = await _http.GetAsync(
            $"api/BinRanges/{binRangeId}", cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound) return null;

        response.EnsureSuccessStatusCode();

        return await response.Content
            .ReadFromJsonAsync<BinRangeListItem>(Json, cancellationToken);
    }

    /// <summary>
    /// Fetches one page of ranges whose stored scheme contradicts the prefix, from
    /// <c>GET /api/BinRanges/scheme-mismatches</c>. Each item carries the detector's
    /// suggestion in <c>DetectedScheme</c>.
    /// </summary>
    public async Task<PagedResult<BinRangeListItem>> GetSchemeMismatchesAsync(
        int page, int pageSize, CancellationToken cancellationToken = default)
    {
        await AuthorizeAsync();

        var url = $"api/BinRanges/scheme-mismatches?page={page.ToString(CultureInfo.InvariantCulture)}" +
                  $"&pageSize={pageSize.ToString(CultureInfo.InvariantCulture)}";

        return await _http.GetFromJsonAsync<PagedResult<BinRangeListItem>>(
            url, Json, cancellationToken) ?? new PagedResult<BinRangeListItem>();
    }

    /// <summary>
    /// Fetches the count of scheme-mismatched ranges, for a Home badge. Returns 0 if the
    /// call fails - the badge is decoration, not the control.
    /// </summary>
    public async Task<int> CountSchemeMismatchesAsync(CancellationToken cancellationToken = default)
    {
        await AuthorizeAsync();

        try
        {
            return await _http.GetFromJsonAsync<int>(
                "api/BinRanges/scheme-mismatches/count", Json, cancellationToken);
        }
        catch
        {
            return 0;
        }
    }

    /// <summary>
    /// Fetches the filter dropdown values from <c>GET /api/BinRanges/filters</c>.
    /// </summary>
    public async Task<BinRangeFilterOptions> GetFilterOptionsAsync(
        CancellationToken cancellationToken = default)
    {
        await AuthorizeAsync();

        return await _http.GetFromJsonAsync<BinRangeFilterOptions>(
            "api/BinRanges/filters", Json, cancellationToken) ?? new BinRangeFilterOptions();
    }

    /// <summary>
    /// Adds a BIN range via <c>POST /api/BinRanges</c>.
    /// </summary>
    public Task<BinRangeMutationResult> CreateAsync(
        BinRangeInput input, CancellationToken cancellationToken = default) =>
        SendAsync(HttpMethod.Post, "api/BinRanges", input, cancellationToken);

    /// <summary>
    /// Overwrites a BIN range via <c>PUT /api/BinRanges/{id}</c>.
    /// </summary>
    public Task<BinRangeMutationResult> UpdateAsync(
        int binRangeId, BinRangeInput input, CancellationToken cancellationToken = default) =>
        SendAsync(HttpMethod.Put, $"api/BinRanges/{binRangeId}", input, cancellationToken);

    /// <summary>
    /// Soft-deletes a BIN range via <c>DELETE /api/BinRanges/{id}</c>.
    /// </summary>
    public Task<BinRangeMutationResult> DeleteAsync(
        int binRangeId, CancellationToken cancellationToken = default) =>
        SendAsync(HttpMethod.Delete, $"api/BinRanges/{binRangeId}", null, cancellationToken);

    /// <summary>
    /// Restores a soft-deleted BIN range via <c>POST /api/BinRanges/{id}/restore</c>.
    /// </summary>
    public Task<BinRangeMutationResult> RestoreAsync(
        int binRangeId, CancellationToken cancellationToken = default) =>
        SendAsync(HttpMethod.Post, $"api/BinRanges/{binRangeId}/restore", null, cancellationToken);

    /// <summary>
    /// Runs one write and reads the outcome. A refusal is a normal answer here - the API
    /// returns the same body with a 400, 404 or 409 - so the status code is not thrown on;
    /// the caller reads <c>Status</c> and shows <c>Error</c>.
    /// </summary>
    private async Task<BinRangeMutationResult> SendAsync(
        HttpMethod method, string url, BinRangeInput? body, CancellationToken cancellationToken)
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
        return BinRangeMutationResult.Failure(
            BinRangeMutationStatus.Invalid, await DescribeAsync(response, cancellationToken));
    }

    private static async Task<BinRangeMutationResult?> ReadResultAsync(
        HttpResponseMessage response, CancellationToken cancellationToken)
    {
        try
        {
            var result = await response.Content
                .ReadFromJsonAsync<BinRangeMutationResult>(Json, cancellationToken);

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
        if (response.StatusCode == HttpStatusCode.Unauthorized ||
            response.StatusCode == HttpStatusCode.Forbidden)
        {
            return "You are not allowed to change BIN ranges. Sign in as an admin and try again.";
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

    /// <summary>
    /// Only the filters that are actually set are sent, so the URL stays readable and
    /// an empty filter is never mistaken for a filter on an empty string.
    /// </summary>
    private static string BuildQueryString(BinRangeQuery query)
    {
        var parts = new List<string>
        {
            $"page={query.Page.ToString(CultureInfo.InvariantCulture)}",
            $"pageSize={query.PageSize.ToString(CultureInfo.InvariantCulture)}"
        };

        Add(parts, "prefix", query.Prefix);
        Add(parts, "cardScheme", query.CardScheme);
        Add(parts, "productType", query.ProductType);
        Add(parts, "fundingType", query.FundingType);
        Add(parts, "countryCode", query.CountryCode);
        Add(parts, "createdBy", query.CreatedBy);

        if (query.Status.HasValue)
        {
            parts.Add($"status={query.Status.Value}");
        }

        return string.Join('&', parts);
    }

    private static void Add(List<string> parts, string name, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            parts.Add($"{name}={Uri.EscapeDataString(value.Trim())}");
        }
    }
}
