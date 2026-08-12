using BinTool.Application.Abstractions;
using BinTool.Application.Authorization;
using BinTool.Application.Models.Audit;
using BinTool.Application.Models.BinRanges;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BinTool.Api.Controllers;

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
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<AuditLogItem>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Search(
        [FromQuery] AuditQuery query, CancellationToken cancellationToken)
    {
        var result = await _service.SearchAsync(query, cancellationToken);

        return Ok(result);
    }

    /// <summary>
    /// Lists the entity-type values the audit log records
    /// </summary>
    [HttpGet("entity-types")]
    [ProducesResponseType(typeof(IReadOnlyList<string>), StatusCodes.Status200OK)]
    public IActionResult EntityTypes(CancellationToken cancellationToken)
    {
        _ = cancellationToken;

        return Ok(_service.GetEntityTypes());
    }
}
