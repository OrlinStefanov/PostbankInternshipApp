using BinTool.Core.Authorization;
using BinTool.Core.Models.BinRanges;
using BinTool.Core.Services;
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

    /// <summary>
    /// Lists BIN ranges, filtered and paged.
    /// </summary>
    /// <remarks>
    /// Sample request:
    ///
    ///     GET /api/binranges?prefix=4000&amp;cardScheme=Visa&amp;page=1&amp;pageSize=25
    ///
    /// Every filter is optional and they combine with AND. `prefix` is a starts-with
    /// match, so `4000` finds both 400001 and 40000123. The name filters
    /// (`cardScheme`, `productType`, `fundingType`, `countryCode`) are matched
    /// case-insensitively against the reference data. `createdBy` narrows to the ranges a
    /// given account added, matched case-insensitively; `system` selects rows written with
    /// no user signed in.
    ///
    /// Each row carries a `status` derived from its dates and delete flag rather than
    /// stored, so it cannot fall out of step with them:
    ///
    /// - **Active** - valid today, and what classification will match.
    /// - **Scheduled** - `validFrom` is in the future.
    /// - **Expired** - `validTo` has passed.
    /// - **Deleted** - soft-deleted.
    ///
    /// Filtering by `status` narrows to one of those. Left unset, deleted ranges are
    /// excluded and everything else is returned - so `status=Deleted` is the only way
    /// to see them.
    ///
    /// Each row also reports who added it (`createdBy`, `createdAt`) and who last changed
    /// it (`updatedBy`, `updatedAt`) - normally the user who ran the import, and whoever
    /// later applied a conflict over it. A `createdBy` of `system` is not an account: it
    /// means no user was signed in when the row was written.
    ///
    /// Results are ordered by prefix. `pageSize` is capped at 200; `totalCount` counts
    /// every match rather than just this page, so a client can render a pager without
    /// a second call.
    /// </remarks>
    /// <param name="query">Filters and paging.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
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

    /// <summary>
    /// Lists the reference values available as filters.
    /// </summary>
    /// <remarks>
    /// The card schemes, product types, funding types and countries currently in the
    /// database, so a client can populate its filter dropdowns from data instead of
    /// hard-coding the lists. Soft-deleted reference values are omitted. `creators` lists
    /// the accounts that have added a range - the values the `createdBy` filter takes.
    /// </remarks>
    /// <param name="cancellationToken">Cancellation token.</param>
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
    /// <remarks>
    /// One row per live range where the digits say one network and the stored
    /// <c>cardScheme</c> names another. Each item carries the detector's opinion in
    /// <c>detectedScheme</c>. Rows the detector cannot judge (a prefix in no known IIN
    /// range) are not returned - only genuine contradictions.
    /// </remarks>
    /// <param name="page">1-based page number.</param>
    /// <param name="pageSize">Rows per page, capped at 200.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
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
    /// Total number of scheme-mismatched ranges, for a "vulnerabilities" badge on the home
    /// screen. Same scan as <see cref="SchemeMismatches"/>, without the projection.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
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
    /// Returns one BIN range by id.
    /// </summary>
    /// <remarks>
    /// Soft-deleted ranges are returned too, with `status: Deleted` - otherwise there
    /// would be no way to look at one before deciding whether to restore it.
    /// </remarks>
    /// <param name="id">Id of the range.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
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

    /// <summary>
    /// Adds a single BIN range by hand.
    /// </summary>
    /// <remarks>
    /// For the ranges a CSV does not cover - a one-off correction, or a range that arrives
    /// on its own. Sample request:
    ///
    ///     POST /api/BinRanges
    ///     {
    ///       "prefix": "400001",
    ///       "cardScheme": "Visa",
    ///       "productType": "Consumer",
    ///       "fundingType": "Credit",
    ///       "countryCode": "US",
    ///       "validFrom": "2024-01-01",
    ///       "validTo": null
    ///     }
    ///
    /// `cardScheme`, `productType`, `fundingType` and `countryCode` name existing reference
    /// data, matched case-insensitively, exactly as in the CSV. Naming something that does
    /// not exist is rejected rather than creating it. Leave `validTo` null for an
    /// open-ended range.
    ///
    /// The prefix must be free. One exception: if its only record was soft-deleted, that
    /// row is revived with these values and the response comes back as `Restored` - the
    /// prefix is unique across deleted rows too, so there is no second record to insert.
    ///
    /// The range records the signed-in user as having added it, which is what the browse
    /// listing's "added by" reports.
    ///
    /// The prefix's leading digits are cross-checked against the declared `cardScheme`.
    /// A mismatch is refused with `409 Conflict` and `status: SchemeMismatch` so the
    /// caller can show the reason - the body's `error` names the network the digits belong
    /// to. To save anyway (co-brand block, new allocation, deliberate correction), resend
    /// with `acknowledgeSchemeMismatch: true`; then the row is stored as declared.
    /// </remarks>
    /// <param name="input">The range to add.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
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

        return Respond(result);
    }

    /// <summary>
    /// Overwrites a BIN range with new values.
    /// </summary>
    /// <remarks>
    /// The whole range is replaced, so send every field - anything omitted is treated as
    /// cleared, not left alone. The prefix may be changed as long as no other range owns
    /// it. The range keeps its id, and the signed-in user is recorded as its last editor.
    ///
    /// A soft-deleted range cannot be edited: restore it first, so bringing it back is
    /// never a side effect of a correction.
    ///
    /// The same prefix-vs-scheme cross-check as on insert: a mismatch is refused with
    /// `409 Conflict` and `status: SchemeMismatch`. Resend with
    /// `acknowledgeSchemeMismatch: true` to save it anyway.
    /// </remarks>
    /// <param name="id">Id of the range to edit.</param>
    /// <param name="input">The new values.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
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

        return Respond(result);
    }

    /// <summary>
    /// Withdraws a BIN range.
    /// </summary>
    /// <remarks>
    /// The delete is soft. The range stops matching classification and drops out of the
    /// default listing, but the record stays - `GET /api/BinRanges?status=Deleted` still
    /// finds it, and `POST /api/BinRanges/{id}/restore` brings it back with its history.
    ///
    /// The prefix stays reserved while the range is deleted. Adding it again revives this
    /// record rather than creating a second one.
    /// </remarks>
    /// <param name="id">Id of the range to withdraw.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
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

        return Respond(result);
    }

    /// <summary>
    /// Brings a soft-deleted BIN range back.
    /// </summary>
    /// <remarks>
    /// The range returns with the values it had when it was deleted, and starts matching
    /// classification again from the moment it is restored, subject to its own dates.
    /// </remarks>
    /// <param name="id">Id of the range to restore.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
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

        return Respond(result);
    }

    /// <summary>
    /// Maps a refusal onto the status code that says the same thing. Every write returns
    /// the same body either way, so a client reads one shape rather than two.
    /// </summary>
    private IActionResult Respond(BinRangeMutationResult result) => result.Status switch
    {
        BinRangeMutationStatus.NotFound => NotFound(result),
        BinRangeMutationStatus.PrefixInUse => Conflict(result),
        BinRangeMutationStatus.AlreadyInThatState => Conflict(result),
        BinRangeMutationStatus.SchemeMismatch => Conflict(result),
        BinRangeMutationStatus.Invalid => BadRequest(result),
        _ => Ok(result)
    };
}
