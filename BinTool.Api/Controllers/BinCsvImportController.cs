using BinTool.Core.Models.Import;
using BinTool.Core.Services;
using Microsoft.AspNetCore.Mvc;

namespace BinTool.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BinCsvImportController : ControllerBase
{
    private readonly IBinCsvImportService _service;

    public BinCsvImportController(IBinCsvImportService service)
    {
        _service = service;
    }

    /// <summary>
    /// Imports a BIN CSV: new prefixes are inserted, rejected rows are recorded,
    /// unchanged rows are skipped, and prefixes that already exist with different
    /// values are returned as conflicts for the caller to resolve.
    /// </summary>
    [HttpPost("import")]
    public async Task<IActionResult> Import(IFormFile file, CancellationToken cancellationToken)
    {
        if (file == null || file.Length == 0) return BadRequest("Invalid file");

        await using var stream = file.OpenReadStream();

        var result = await _service.ImportAsync(stream, file.FileName, cancellationToken);

        return Ok(result);
    }

    /// <summary>
    /// Returns the conflicts still awaiting a decision, so the UI can restore the
    /// resolution workflow after a page reload.
    /// </summary>
    [HttpGet("conflicts")]
    public async Task<IActionResult> GetConflicts(CancellationToken cancellationToken)
    {
        var conflicts = await _service.GetPendingConflictsAsync(cancellationToken);
        return Ok(conflicts);
    }

    /// <summary>
    /// Applies the user's per-conflict decisions from a previous import: each
    /// conflict is either used to overwrite the existing BIN range or discarded.
    /// </summary>
    [HttpPost("resolve-conflicts")]
    public async Task<IActionResult> ResolveConflicts(
        [FromBody] IEnumerable<ConflictResolution> resolutions, CancellationToken cancellationToken)
    {
        if (resolutions == null) return BadRequest("No resolutions provided");

        var result = await _service.ResolveConflictsAsync(resolutions, cancellationToken);

        return Ok(result);
    }
}
