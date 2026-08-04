using BinTool.Core.Models.BinRanges;
using BinTool.Core.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BinTool.Api.Controllers;

/// <summary>
/// Browses the stored BIN ranges.
/// <para>
/// Open to any signed-in user: browsing only reads.
/// </para>
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[Authorize]
public class BinRangesController : ControllerBase
{
    private readonly IBinRangeQueryService _service;

    public BinRangesController(IBinRangeQueryService service)
    {
        _service = service;
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
    /// case-insensitively against the reference data.
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
    /// hard-coding the lists. Soft-deleted reference values are omitted.
    /// </remarks>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="200">The available filter values.</response>
    [HttpGet("filters")]
    [ProducesResponseType(typeof(BinRangeFilterOptions), StatusCodes.Status200OK)]
    public async Task<IActionResult> Filters(CancellationToken cancellationToken)
    {
        var options = await _service.GetFilterOptionsAsync(cancellationToken);

        return Ok(options);
    }
}
