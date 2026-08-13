using BinTool.Api.Errors;
using BinTool.Application.Abstractions;
using BinTool.Application.Authorization;
using BinTool.Application.Models.BinRanges;
using BinTool.Application.Models.Import;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BinTool.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[Authorize(Policy = Permissions.BinRangesImport)]
public class BinCsvImportController : ControllerBase
{
    private readonly IBinCsvImportService _service;
    private readonly IImportHistoryQueryService _history;

    public BinCsvImportController(
        IBinCsvImportService service, IImportHistoryQueryService history)
    {
        _service = service;
        _history = history;
    }

    private const long MaxUploadBytes = 10 * 1024 * 1024;

    /// <summary>Imports a CSV of BIN ranges.</summary>
    [HttpPost("import")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(MaxUploadBytes)]
    [ProducesResponseType(typeof(BinImportResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Import(IFormFile file, CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            return ApiResults.Invalid("A CSV file is required.");
        }

        if (file.Length > MaxUploadBytes)
        {
            return ApiResults.Invalid($"The file exceeds the {MaxUploadBytes / (1024 * 1024)} MB limit.");
        }

        if (!Path.GetExtension(file.FileName).Equals(".csv", StringComparison.OrdinalIgnoreCase))
        {
            return ApiResults.Invalid("Only .csv files are accepted.");
        }

        await using var stream = file.OpenReadStream();

        var result = await _service.ImportAsync(stream, file.FileName, cancellationToken);

        return Ok(result);
    }

    /// <summary>Lists the conflicts still awaiting a decision.</summary>
    [HttpGet("conflicts")]
    [ProducesResponseType(typeof(List<BinConflict>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetConflicts(CancellationToken cancellationToken)
    {
        var conflicts = await _service.GetPendingConflictsAsync(cancellationToken);
        return Ok(conflicts);
    }

    /// <summary>Lists past imports, filtered and paged.</summary>
    [HttpGet("history")]
    [ProducesResponseType(typeof(PagedResult<ImportHistoryItem>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetHistory(
        [FromQuery] ImportHistoryQuery query, CancellationToken cancellationToken)
    {
        var result = await _history.SearchAsync(query, cancellationToken);
        return Ok(result);
    }

    /// <summary>Applies update/discard decisions to staged conflicts.</summary>
    [HttpPost("resolve-conflicts")]
    [ProducesResponseType(typeof(ConflictResolutionResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ResolveConflicts(
        [FromBody] IEnumerable<ConflictResolution> resolutions, CancellationToken cancellationToken)
    {
        if (resolutions is null)
        {
            return ApiResults.Invalid("At least one conflict resolution is required.");
        }

        var result = await _service.ResolveConflictsAsync(resolutions, cancellationToken);

        return Ok(result);
    }
}
