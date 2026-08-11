using BinTool.Application.Authorization;
using BinTool.Application.Models.Audit;
using BinTool.Application.Models.BinRanges;
using BinTool.Application.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BinTool.Api.Controllers;

/// <summary>
/// Browses the audit trail. Read-only by design - audit rows are written by the same
/// SaveChanges as the change they describe, and there is no path to edit or delete one
/// through the application.
/// <para>
/// Requires the <c>audit.read</c> permission. Admin holds it by default; a future role
/// composed of just this permission can watch the trail without holding write access.
/// </para>
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[Authorize(Policy = Permissions.AuditRead)]
public class AuditController : ControllerBase
{
    private readonly IAuditQueryService _service;

    public AuditController(IAuditQueryService service)
    {
        _service = service;
    }

    /// <summary>
    /// Lists audit entries, filtered and paged.
    /// </summary>
    /// <remarks>
    /// Sample request:
    ///
    ///     GET /api/audit?entityType=BinRange&amp;from=2026-08-01T00:00:00Z&amp;page=1&amp;pageSize=25
    ///
    /// Every filter is optional and they combine with AND. Dates are UTC: <c>from</c> is
    /// inclusive, <c>to</c> is exclusive, so two adjacent day queries never claim the
    /// same row twice. <c>entityType</c> is one of the values from
    /// <c>GET /api/audit/entity-types</c>, matched case-insensitively.
    ///
    /// <c>userName</c> is a starts-with match against the Identity user name. The literal
    /// <c>system</c> selects rows written with no user signed in - it is not an account.
    ///
    /// Results are ordered by <c>performedAt</c> descending, then by id descending so
    /// two rows written in the same tick stay in a stable order across pages.
    ///
    /// <c>oldValues</c> and <c>newValues</c> are the JSON snapshots the writer stored.
    /// They are returned as raw strings - the client decides how to render them.
    ///
    /// <c>pageSize</c> is capped at 200; <c>totalCount</c> counts every match rather
    /// than just this page so a client can render a pager without a second call.
    /// </remarks>
    /// <param name="query">Filters and paging.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="200">One page of matching entries. Empty when nothing matched.</response>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<AuditLogItem>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Search(
        [FromQuery] AuditQuery query, CancellationToken cancellationToken)
    {
        var result = await _service.SearchAsync(query, cancellationToken);

        return Ok(result);
    }

    /// <summary>
    /// Lists the entity-type values the audit log records - the values the
    /// <c>entityType</c> filter accepts. Sourced from the canonical constants rather than
    /// from the data, so an option exists even before its first row is written.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="200">The available entity-type values, ordered.</response>
    [HttpGet("entity-types")]
    [ProducesResponseType(typeof(IReadOnlyList<string>), StatusCodes.Status200OK)]
    public IActionResult EntityTypes(CancellationToken cancellationToken)
    {
        _ = cancellationToken;

        return Ok(_service.GetEntityTypes());
    }
}
