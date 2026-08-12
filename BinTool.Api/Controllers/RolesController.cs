using BinTool.Api.Errors;
using BinTool.Application.Abstractions;
using BinTool.Application.Authorization;
using BinTool.Application.Models.Access;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BinTool.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[Authorize(Policy = Permissions.RolesManage)]
public class RolesController : ControllerBase
{
    private readonly IRoleAdminService _service;

    public RolesController(IRoleAdminService service)
    {
        _service = service;
    }

    /// <summary>Lists all roles, each with its permissions and how many users hold it.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<RoleListItem>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRoles(CancellationToken cancellationToken)
    {
        return Ok(await _service.GetRolesAsync(cancellationToken));
    }

    /// <summary>Lists the permission catalog a role can be composed from.</summary>
    [HttpGet("permissions")]
    [ProducesResponseType(typeof(IReadOnlyList<PermissionInfo>), StatusCodes.Status200OK)]
    public IActionResult GetPermissions()
    {
        return Ok(_service.GetPermissionCatalog());
    }

    /// <summary>Creates a role.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(RoleMutationResult), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(RoleMutationResult), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(
        [FromBody] RoleInput input, CancellationToken cancellationToken)
    {
        var result = await _service.CreateAsync(input, cancellationToken);

        if (result.Status == RoleMutationStatus.Created)
        {
            return CreatedAtAction(nameof(GetRoles), result);
        }

        return ApiResults.From(result);
    }

    /// <summary>Overwrites a role's name, description and permissions.</summary>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(RoleMutationResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(RoleMutationResult), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(RoleMutationResult), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(
        string id, [FromBody] RoleInput input, CancellationToken cancellationToken)
    {
        return ApiResults.From(await _service.UpdateAsync(id, input, cancellationToken));
    }

    /// <summary>Deletes a role.</summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(typeof(RoleMutationResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RoleMutationResult), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(RoleMutationResult), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(string id, CancellationToken cancellationToken)
    {
        return ApiResults.From(await _service.DeleteAsync(id, cancellationToken));
    }

}
