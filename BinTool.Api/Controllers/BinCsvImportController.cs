using BinTool.Api.Errors;
using BinTool.Application.Abstractions;
using BinTool.Application.Authorization;
using BinTool.Application.Models.BinRanges;
using BinTool.Application.Models.Import;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BinTool.Api.Controllers;

/// <summary>
/// Imports BIN ranges from CSV and resolves the conflicts an import stages.
/// <para>
/// Requires the <c>binranges.import</c> permission: every endpoint here writes to, or decides
/// the fate of, live BIN data.
/// </para>
/// </summary>
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

    /// <summary>Imports a CSV of BIN ranges.</summary>
    /// <param name="file">The CSV file, sent as multipart/form-data under the field <c>file</c>.</param>
    /// <response code="200">
    /// The import ran. The body reports the per-bucket counts, any staged conflicts and
    /// the rejected rows. A 200 does not mean every row was accepted - check the counts.
    /// </response>
    /// <response code="400">No file was supplied, or the file was empty.</response>
    [HttpPost("import")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(BinImportResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Import(IFormFile file, CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            return ApiResults.Invalid("A CSV file is required.");
        }

        await using var stream = file.OpenReadStream();

        var result = await _service.ImportAsync(stream, file.FileName, cancellationToken);

        return Ok(result);
    }

    /// <summary>Lists the conflicts still awaiting a decision.</summary>
    /// <response code="200">The pending conflicts, each with its per-field diff. Empty if none.</response>
    [HttpGet("conflicts")]
    [ProducesResponseType(typeof(List<BinConflict>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetConflicts(CancellationToken cancellationToken)
    {
        var conflicts = await _service.GetPendingConflictsAsync(cancellationToken);
        return Ok(conflicts);
    }

    /// <summary>Lists past imports, filtered and paged.</summary>
    /// <param name="query">Filters and paging.</param>
    /// <response code="200">One page of matching imports. Empty when nothing matched.</response>
    [HttpGet("history")]
    [ProducesResponseType(typeof(PagedResult<ImportHistoryItem>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetHistory(
        [FromQuery] ImportHistoryQuery query, CancellationToken cancellationToken)
    {
        var result = await _history.SearchAsync(query, cancellationToken);
        return Ok(result);
    }

    /// <summary>Applies update/discard decisions to staged conflicts.</summary>
    /// <param name="resolutions">The decision for each conflict.</param>
    /// <response code="200">Counts of how many conflicts were updated, discarded and not found.</response>
    /// <response code="400">No request body was supplied.</response>
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
