using System.Globalization;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using BinTool.Core.Models.BinRanges;

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
