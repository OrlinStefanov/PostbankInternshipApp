using System.Globalization;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using BinTool.Application.Models.BinRanges;
using BinTool.Application.Models.Import;

namespace BinTool.UI.Services;

public class BinImportApiClient
{
    private readonly HttpClient _http;
    private readonly IAccessTokenProvider _tokens;

    public BinImportApiClient(HttpClient http, IAccessTokenProvider tokens)
    {
        _http = http;
        _tokens = tokens;
    }

    // Attaches the signed-in user's token. Safe to set on the instance: a typed client gets its own
    // "HttpClient", so this never leaks across users.
    private async Task AuthorizeAsync()
    {
        var token = await _tokens.GetTokenAsync();

        _http.DefaultRequestHeaders.Authorization = token is null
            ? null
            : new AuthenticationHeaderValue("Bearer", token);
    }

    public async Task<BinImportResult> ImportAsync(
        Stream content, string fileName, CancellationToken cancellationToken = default)
    {
        await AuthorizeAsync();

        using var form = new MultipartFormDataContent();
        var file = new StreamContent(content);
        file.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("text/csv");

        // The API binds the IFormFile from the field named "file".
        form.Add(file, "file", fileName);

        var response = await _http.PostAsync("api/BinCsvImport/import", form, cancellationToken);
        response.EnsureSuccessStatusCode();

        return (await response.Content.ReadFromJsonAsync<BinImportResult>(cancellationToken))!;
    }

    public async Task<List<BinConflict>> GetConflictsAsync(CancellationToken cancellationToken = default)
    {
        await AuthorizeAsync();

        return await _http.GetFromJsonAsync<List<BinConflict>>(
            "api/BinCsvImport/conflicts", cancellationToken) ?? new List<BinConflict>();
    }

    public async Task<ConflictResolutionResult> ResolveConflictsAsync(
        IEnumerable<ConflictResolution> resolutions, CancellationToken cancellationToken = default)
    {
        await AuthorizeAsync();

        var response = await _http.PostAsJsonAsync(
            "api/BinCsvImport/resolve-conflicts", resolutions, cancellationToken);
        response.EnsureSuccessStatusCode();

        return (await response.Content.ReadFromJsonAsync<ConflictResolutionResult>(cancellationToken))!;
    }

    public async Task<PagedResult<ImportHistoryItem>> GetHistoryAsync(
        ImportHistoryQuery query, CancellationToken cancellationToken = default)
    {
        await AuthorizeAsync();

        var result = await _http.GetFromJsonAsync<PagedResult<ImportHistoryItem>>(
            $"api/BinCsvImport/history?{BuildQueryString(query)}", cancellationToken);

        return result ?? new PagedResult<ImportHistoryItem>();
    }

    // Only the filters that are set are sent, so an empty filter is never mistaken for a filter on
    // an empty string.
    private static string BuildQueryString(ImportHistoryQuery query)
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

        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            parts.Add($"status={Uri.EscapeDataString(query.Status.Trim())}");
        }

        return string.Join('&', parts);
    }
}
