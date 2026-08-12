using BinTool.Api.Errors;
using BinTool.Application.Abstractions;
using BinTool.Application.Authorization;
using BinTool.Application.Models.BinRanges;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BinTool.Api.Controllers;

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
    [HttpGet("filters")]
    [Authorize(Policy = Permissions.BinRangesRead)]
    [ProducesResponseType(typeof(BinRangeFilterOptions), StatusCodes.Status200OK)]
    public async Task<IActionResult> Filters(CancellationToken cancellationToken)
    {
        var options = await _service.GetFilterOptionsAsync(cancellationToken);

        return Ok(options);
    }

    /// <summary>
    /// Lists BIN ranges whose stored card scheme contradicts the network the detector assigns to the prefix.
    /// </summary>
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
    /// Total number of scheme-mismatched ranges, for the mismatch badge on the home screen.
    /// </summary>
    [HttpGet("scheme-mismatches/count")]
    [Authorize(Policy = Permissions.BinRangesRead)]
    [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
    public async Task<IActionResult> SchemeMismatchCount(CancellationToken cancellationToken)
    {
        var count = await _service.CountSchemeMismatchesAsync(cancellationToken);
        return Ok(count);
    }

    /// <summary>
    /// What the card-scheme detector makes of a prefix, for an editor suggesting the scheme as it is typed.
    /// </summary>
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
}
