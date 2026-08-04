using System.Net.Http.Headers;
using System.Net.Http.Json;
using BinTool.Core.Models.Import;

namespace BinTool.UI.Services;

/// <summary>
/// Thin typed client over the BinTool API's CSV import endpoints. Runs on the
/// Blazor server, so calls are server-to-server (no CORS involved).
/// </summary>
public class BinImportApiClient
{
    private readonly HttpClient _http;
    private readonly IAccessTokenProvider _tokens;

    public BinImportApiClient(HttpClient http, IAccessTokenProvider tokens)
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
    /// Uploads a CSV to <c>POST /api/BinCsvImport/import</c>.
    /// </summary>
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

    /// <summary>
    /// Fetches the conflicts still awaiting a decision from
    /// <c>GET /api/BinCsvImport/conflicts</c>. Used to restore state after a reload.
    /// </summary>
    public async Task<List<BinConflict>> GetConflictsAsync(CancellationToken cancellationToken = default)
    {
        await AuthorizeAsync();

        return await _http.GetFromJsonAsync<List<BinConflict>>(
            "api/BinCsvImport/conflicts", cancellationToken) ?? new List<BinConflict>();
    }

    /// <summary>
    /// Applies per-conflict decisions via <c>POST /api/BinCsvImport/resolve-conflicts</c>.
    /// </summary>
    public async Task<ConflictResolutionResult> ResolveConflictsAsync(
        IEnumerable<ConflictResolution> resolutions, CancellationToken cancellationToken = default)
    {
        await AuthorizeAsync();

        var response = await _http.PostAsJsonAsync(
            "api/BinCsvImport/resolve-conflicts", resolutions, cancellationToken);
        response.EnsureSuccessStatusCode();

        return (await response.Content.ReadFromJsonAsync<ConflictResolutionResult>(cancellationToken))!;
    }
}
