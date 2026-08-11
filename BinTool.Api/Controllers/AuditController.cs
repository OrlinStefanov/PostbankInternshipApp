using BinTool.Application.Abstractions;
using BinTool.Application.Authorization;
using BinTool.Application.Models.Audit;
using BinTool.Application.Models.BinRanges;
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

    /// <summary>Lists audit entries, filtered and paged.</summary>
    /// <param name="query">Filters and paging.</param>
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
    /// <response code="200">The available entity-type values, ordered.</response>
    [HttpGet("entity-types")]
    [ProducesResponseType(typeof(IReadOnlyList<string>), StatusCodes.Status200OK)]
    public IActionResult EntityTypes(CancellationToken cancellationToken)
    {
        _ = cancellationToken;

        return Ok(_service.GetEntityTypes());
    }
}
