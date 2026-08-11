using System.Globalization;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using BinTool.Application.Models.Audit;
using BinTool.Application.Models.BinRanges;
using BinTool.Domain.Entities;

namespace BinTool.UI.Services;

public class AuditApiClient
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly HttpClient _http;
    private readonly IAccessTokenProvider _tokens;

    public AuditApiClient(HttpClient http, IAccessTokenProvider tokens)
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

    public async Task<PagedResult<AuditLogItem>> SearchAsync(
        AuditQuery query, CancellationToken cancellationToken = default)
    {
        await AuthorizeAsync();

        var result = await _http.GetFromJsonAsync<PagedResult<AuditLogItem>>(
            $"api/Audit?{BuildQueryString(query)}", Json, cancellationToken);

        return result ?? new PagedResult<AuditLogItem>();
    }

    public async Task<IReadOnlyList<string>> GetEntityTypesAsync(
        CancellationToken cancellationToken = default)
    {
        await AuthorizeAsync();

        var result = await _http.GetFromJsonAsync<List<string>>(
            "api/Audit/entity-types", Json, cancellationToken);

        return result ?? new List<string>();
    }

    // Only the filters that are set are sent, so the URL stays readable and an empty filter is
    // never mistaken for a filter on an empty string.
    private static string BuildQueryString(AuditQuery query)
    {
        var parts = new List<string>
        {
            $"page={query.Page.ToString(CultureInfo.InvariantCulture)}",
            $"pageSize={query.PageSize.ToString(CultureInfo.InvariantCulture)}"
        };

        if (query.From is { } from)
        {
            parts.Add($"from={Uri.EscapeDataString(from.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture))}");
        }

        if (query.To is { } to)
        {
            parts.Add($"to={Uri.EscapeDataString(to.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture))}");
        }

        Add(parts, "entityType", query.EntityType);
        Add(parts, "userName", query.UserName);

        if (query.Action is { } action)
        {
            parts.Add($"action={action}");
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
