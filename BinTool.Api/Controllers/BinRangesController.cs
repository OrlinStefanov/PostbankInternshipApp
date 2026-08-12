using BinTool.Api.Errors;
using BinTool.Application.Abstractions;
using BinTool.Application.Authorization;
using BinTool.Application.Models.BinRanges;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BinTool.Api.Controllers;

/// <summary>
/// Browses and maintains the stored BIN ranges.
/// <para>
/// Reading is open to any signed-in user. Adding, editing, deleting and restoring a
/// range are Admin only - they change live BIN data, so they carry the same access rule
/// as importing.
/// </para>
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[Authorize]
public class BinRangesController : ControllerBase
{
    private readonly IBinRangeQueryService _service;
    private readonly IBinRangeAdminService _admin;

    public BinRangesController(IBinRangeQueryService service, IBinRangeAdminService admin)
    {
        _service = service;
        _admin = admin;
    }

    /// <summary>Lists BIN ranges, filtered and paged.</summary>
    /// <param name="query">Filters and paging.</param>
    /// <response code="200">One page of matching ranges. Empty when nothing matched.</response>
    [HttpGet]
    [Authorize(Policy = Permissions.BinRangesRead)]
    [ProducesResponseType(typeof(PagedResult<BinRangeListItem>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Search(
        [FromQuery] BinRangeQuery query, CancellationToken cancellationToken)
    {
        var result = await _service.SearchAsync(query, cancellationToken);

        return Ok(result);
    }

    /// <summary>Lists the reference values available as filters.</summary>
    /// <response code="200">The available filter values.</response>
    [HttpGet("filters")]
    [Authorize(Policy = Permissions.BinRangesRead)]
    [ProducesResponseType(typeof(BinRangeFilterOptions), StatusCodes.Status200OK)]
    public async Task<IActionResult> Filters(CancellationToken cancellationToken)
    {
        var options = await _service.GetFilterOptionsAsync(cancellationToken);

        return Ok(options);
    }

    /// <summary>
    /// Lists BIN ranges whose stored card scheme contradicts the network the detector
    /// assigns to the prefix.
    /// </summary>
    /// <param name="page">1-based page number.</param>
    /// <param name="pageSize">Rows per page, capped at 200.</param>
    /// <response code="200">One page of mismatched ranges. Empty when nothing is wrong.</response>
    [HttpGet("scheme-mismatches")]
    [Authorize(Policy = Permissions.BinRangesRead)]
    [ProducesResponseType(typeof(PagedResult<BinRangeListItem>), StatusCodes.Status200OK)]
    public async Task<IActionResult> SchemeMismatches(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = BinRangeQuery.DefaultPageSize,
        CancellationToken cancellationToken = default)
    {
        var result = await _service.GetSchemeMismatchesAsync(page, pageSize, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Total number of scheme-mismatched ranges, for the mismatch badge on the home
    /// screen. Same scan as <see cref="SchemeMismatches"/>, without the projection.
    /// </summary>
    /// <response code="200">The count.</response>
    [HttpGet("scheme-mismatches/count")]
    [Authorize(Policy = Permissions.BinRangesRead)]
    [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
    public async Task<IActionResult> SchemeMismatchCount(CancellationToken cancellationToken)
    {
        var count = await _service.CountSchemeMismatchesAsync(cancellationToken);
        return Ok(count);
    }

    /// <summary>
    /// What the card-scheme detector makes of a prefix, for an editor suggesting the scheme as
    /// it is typed.
    /// </summary>
    /// <param name="prefix">The BIN prefix, 6-19 digits.</param>
    /// <param name="declaredScheme">The scheme currently chosen, if any.</param>
    /// <response code="200">
    /// The reading. <c>detectedScheme</c> is null when the prefix matches no published range.
    /// </response>
    /// <response code="400">The prefix is missing, holds a non-digit, or is not 6-19 digits long.</response>
    [HttpGet("scheme-hint")]
    [Authorize(Policy = Permissions.BinRangesRead)]
    [ProducesResponseType(typeof(SchemeHint), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public IActionResult DetectScheme(
        [FromQuery] string prefix, [FromQuery] string? declaredScheme = null)
    {
        return Ok(_service.DetectScheme(prefix, declaredScheme));
    }

    /// <summary>Returns one BIN range by id.</summary>
    /// <param name="id">Id of the range.</param>
    /// <response code="200">The range.</response>
    /// <response code="404">No range has that id.</response>
    [HttpGet("{id:int}", Name = nameof(GetById))]
    [Authorize(Policy = Permissions.BinRangesRead)]
    [ProducesResponseType(typeof(BinRangeListItem), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
    {
        var range = await _admin.GetAsync(id, cancellationToken);

        return range is null ? NotFound() : Ok(range);
    }

    /// <summary>Adds a single BIN range by hand.</summary>
    /// <param name="input">The range to add.</param>
    /// <response code="201">Added. The body carries the stored range.</response>
    /// <response code="200">An existing soft-deleted range was revived with these values.</response>
    /// <response code="400">A field broke a rule, or named reference data that does not exist.</response>
    /// <response code="409">A live range already owns that prefix, or the declared scheme contradicts the prefix.</response>
    [HttpPost]
    [Authorize(Policy = Permissions.BinRangesWrite)]
    [ProducesResponseType(typeof(BinRangeMutationResult), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(BinRangeMutationResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(BinRangeMutationResult), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(
        [FromBody] BinRangeInput input, CancellationToken cancellationToken)
    {
        var result = await _admin.CreateAsync(input, cancellationToken);

        if (result.Status == BinRangeMutationStatus.Created)
        {
            return CreatedAtRoute(nameof(GetById), new { id = result.Range!.BinRangeId }, result);
        }

        return ApiResults.From(result);
    }

    /// <summary>Overwrites a BIN range with new values.</summary>
    /// <param name="id">Id of the range to edit.</param>
    /// <param name="input">The new values.</param>
    /// <response code="200">Updated. The body carries the stored range.</response>
    /// <response code="400">A field broke a rule, or named reference data that does not exist.</response>
    /// <response code="404">No such range, or it is deleted and must be restored first.</response>
    /// <response code="409">Another range already owns that prefix, or the declared scheme contradicts the prefix.</response>
    [HttpPut("{id:int}")]
    [Authorize(Policy = Permissions.BinRangesWrite)]
    [ProducesResponseType(typeof(BinRangeMutationResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(BinRangeMutationResult), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(BinRangeMutationResult), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(
        int id, [FromBody] BinRangeInput input, CancellationToken cancellationToken)
    {
        var result = await _admin.UpdateAsync(id, input, cancellationToken);

        return ApiResults.From(result);
    }

    /// <summary>Withdraws a BIN range.</summary>
    /// <param name="id">Id of the range to withdraw.</param>
    /// <response code="200">Deleted. The body carries the range with `status: Deleted`.</response>
    /// <response code="404">No range has that id.</response>
    /// <response code="409">The range is already deleted.</response>
    [HttpDelete("{id:int}")]
    [Authorize(Policy = Permissions.BinRangesWrite)]
    [ProducesResponseType(typeof(BinRangeMutationResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BinRangeMutationResult), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(BinRangeMutationResult), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var result = await _admin.DeleteAsync(id, cancellationToken);

        return ApiResults.From(result);
    }

    /// <summary>Brings a soft-deleted BIN range back.</summary>
    /// <param name="id">Id of the range to restore.</param>
    /// <response code="200">Restored. The body carries the stored range.</response>
    /// <response code="404">No range has that id.</response>
    /// <response code="409">The range was not deleted.</response>
    [HttpPost("{id:int}/restore")]
    [Authorize(Policy = Permissions.BinRangesWrite)]
    [ProducesResponseType(typeof(BinRangeMutationResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BinRangeMutationResult), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(BinRangeMutationResult), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Restore(int id, CancellationToken cancellationToken)
    {
        var result = await _admin.RestoreAsync(id, cancellationToken);

        return ApiResults.From(result);
    }

    // Maps a refusal onto the status code that says the same thing. Every write returns the same
    // body either way, so a client reads one shape rather than two.
}
