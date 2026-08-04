using BinTool.Core.Entities;
using BinTool.Core.Models.Import;
using BinTool.Core.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BinTool.Api.Controllers;

/// <summary>
/// Imports BIN ranges from CSV and resolves the conflicts an import stages.
/// <para>
/// Admin only: every endpoint here writes to, or decides the fate of, live BIN data.
/// </para>
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[Authorize(Roles = AppRoles.Admin)]
public class BinCsvImportController : ControllerBase
{
    private readonly IBinCsvImportService _service;

    public BinCsvImportController(IBinCsvImportService service)
    {
        _service = service;
    }

    /// <summary>
    /// Imports a CSV of BIN ranges.
    /// </summary>
    /// <remarks>
    /// The file needs a header row and one BIN range per line:
    ///
    ///     Prefix,CardScheme,ProductType,FundingType,CountryCode,ValidFrom,ValidTo
    ///     400001,Visa,Consumer,Credit,US,2024-01-01,2026-12-31
    ///     520082,Mastercard,Commercial,Debit,BG,2024-01-01,
    ///
    /// `Prefix` is 6-8 digits. `CardScheme`, `ProductType`, `FundingType` and
    /// `CountryCode` must match existing reference data (matched case-insensitively);
    /// anything else is rejected rather than created. Dates are `yyyy-MM-dd`, and the
    /// trailing `ValidTo` column is optional - leave it blank for an open-ended range.
    ///
    /// Every row lands in exactly one bucket:
    ///
    /// - **Inserted** - the prefix is new. A prefix whose only record was soft-deleted
    ///   is revived in place rather than duplicated, and counts here.
    /// - **Unchanged** - the prefix exists and every field already matches; skipped.
    /// - **Conflict** - the prefix exists with different values. Nothing is overwritten:
    ///   the row is staged with a per-field diff for a decision, via
    ///   `POST /api/BinCsvImport/resolve-conflicts`.
    /// - **Rejected** - malformed, a duplicate of an earlier row in the same file, or
    ///   referencing reference data that does not exist. Recorded with the reason and
    ///   the original line.
    ///
    /// Inserts, revivals, rejections and staged conflicts are all written in a single
    /// save, along with a summary in the import history.
    /// </remarks>
    /// <param name="file">The CSV file, sent as multipart/form-data under the field <c>file</c>.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="200">
    /// The import ran. The body reports the per-bucket counts, any staged conflicts and
    /// the rejected rows. A 200 does not mean every row was accepted - check the counts.
    /// </response>
    /// <response code="400">No file was supplied, or the file was empty.</response>
    [HttpPost("import")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(BinImportResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Import(IFormFile file, CancellationToken cancellationToken)
    {
        if (file == null || file.Length == 0) return BadRequest("Invalid file");

        await using var stream = file.OpenReadStream();

        var result = await _service.ImportAsync(stream, file.FileName, cancellationToken);

        return Ok(result);
    }

    /// <summary>
    /// Lists the conflicts still awaiting a decision.
    /// </summary>
    /// <remarks>
    /// Staged conflicts are persisted, so this rebuilds the outstanding worklist without
    /// re-uploading the file - which is what lets a client restore the review screen after
    /// a reload. Each diff is recomputed against the record as it stands now, so it stays
    /// accurate if the underlying range changed since the import. Conflicts that have been
    /// applied or discarded are not returned.
    /// </remarks>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="200">The pending conflicts, each with its per-field diff. Empty if none.</response>
    [HttpGet("conflicts")]
    [ProducesResponseType(typeof(List<BinConflict>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetConflicts(CancellationToken cancellationToken)
    {
        var conflicts = await _service.GetPendingConflictsAsync(cancellationToken);
        return Ok(conflicts);
    }

    /// <summary>
    /// Applies update/discard decisions to staged conflicts.
    /// </summary>
    /// <remarks>
    /// Send one entry per conflict:
    ///
    ///     [
    ///       { "pendingBinConflictId": 12, "update": true },
    ///       { "pendingBinConflictId": 13, "update": false }
    ///     ]
    ///
    /// `update: true` writes the imported values onto the existing BIN range, keeping the
    /// same record and its audit trail. `update: false` keeps the stored record as-is and
    /// discards the imported row. Either way the conflict is closed and stops appearing in
    /// `GET /api/BinCsvImport/conflicts`.
    ///
    /// Ids that do not exist, or that were already resolved, are counted under
    /// `notFoundCount` instead of failing the request - so retrying a batch is safe. If the
    /// same id appears twice, the last decision wins.
    /// </remarks>
    /// <param name="resolutions">The decision for each conflict.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="200">Counts of how many conflicts were updated, discarded and not found.</response>
    /// <response code="400">No request body was supplied.</response>
    [HttpPost("resolve-conflicts")]
    [ProducesResponseType(typeof(ConflictResolutionResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ResolveConflicts(
        [FromBody] IEnumerable<ConflictResolution> resolutions, CancellationToken cancellationToken)
    {
        if (resolutions == null) return BadRequest("No resolutions provided");

        var result = await _service.ResolveConflictsAsync(resolutions, cancellationToken);

        return Ok(result);
    }
}
