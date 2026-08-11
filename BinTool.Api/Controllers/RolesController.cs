using BinTool.Application.Authorization;
using BinTool.Application.Models.Access;
using BinTool.Application.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BinTool.Api.Controllers;

/// <summary>
/// Manages roles and the permissions they grant.
/// <para>
/// Requires the <c>roles.manage</c> permission - in practice an admin, though it can be granted
/// to a custom role. The Admin role itself is protected here: it can't be renamed, deleted or
/// have its permissions changed, and it holds every permission implicitly.
/// </para>
/// </summary>
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

    /// <summary>
    /// Lists all roles, each with its permissions and how many users hold it.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="200">The roles.</response>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<RoleListItem>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRoles(CancellationToken cancellationToken)
    {
        return Ok(await _service.GetRolesAsync(cancellationToken));
    }

    /// <summary>
    /// Lists the permission catalog a role can be composed from.
    /// </summary>
    /// <remarks>
    /// The set is fixed in code, because each permission maps to real enforcement on an
    /// endpoint. What is data-driven is which of these a role holds. Each entry carries a group,
    /// so a client can lay the permissions out under headings.
    /// </remarks>
    /// <response code="200">The permission catalog.</response>
    [HttpGet("permissions")]
    [ProducesResponseType(typeof(IReadOnlyList<PermissionInfo>), StatusCodes.Status200OK)]
    public IActionResult GetPermissions()
    {
        return Ok(_service.GetPermissionCatalog());
    }

    /// <summary>
    /// Creates a role.
    /// </summary>
    /// <remarks>
    /// Sample request:
    ///
    ///     POST /api/Roles
    ///     { "name": "Analyst", "description": "Reads and edits BIN ranges",
    ///       "permissions": ["binranges.read", "binranges.write"] }
    ///
    /// The name must be free, and every permission must be a catalog key. Members are assigned
    /// separately, through the users endpoint.
    /// </remarks>
    /// <param name="input">The role to create.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="201">Created. The body carries the stored role.</response>
    /// <response code="400">A field broke a rule, or named an unknown permission.</response>
    /// <response code="409">A role already owns that name.</response>
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

        return Respond(result);
    }

    /// <summary>
    /// Overwrites a role's name, description and permissions.
    /// </summary>
    /// <remarks>
    /// The permissions are replaced wholesale, so send the complete set. The protected Admin
    /// role is refused with a 409.
    /// </remarks>
    /// <param name="id">Id of the role to edit.</param>
    /// <param name="input">The new values.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="200">Updated. The body carries the stored role.</response>
    /// <response code="400">A field broke a rule, or named an unknown permission.</response>
    /// <response code="404">No role has that id.</response>
    /// <response code="409">Another role owns that name, or the role is protected.</response>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(RoleMutationResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(RoleMutationResult), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(RoleMutationResult), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(
        string id, [FromBody] RoleInput input, CancellationToken cancellationToken)
    {
        return Respond(await _service.UpdateAsync(id, input, cancellationToken));
    }

    /// <summary>
    /// Deletes a role.
    /// </summary>
    /// <remarks>
    /// Refused while any user still holds the role, and always for the protected Admin role.
    /// </remarks>
    /// <param name="id">Id of the role to delete.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="200">Deleted.</response>
    /// <response code="404">No role has that id.</response>
    /// <response code="409">The role has members, or is protected.</response>
    [HttpDelete("{id}")]
    [ProducesResponseType(typeof(RoleMutationResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(RoleMutationResult), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(RoleMutationResult), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(string id, CancellationToken cancellationToken)
    {
        return Respond(await _service.DeleteAsync(id, cancellationToken));
    }

    private IActionResult Respond(RoleMutationResult result) => result.Status switch
    {
        RoleMutationStatus.NotFound => NotFound(result),
        RoleMutationStatus.NameInUse => Conflict(result),
        RoleMutationStatus.Protected => Conflict(result),
        RoleMutationStatus.InUse => Conflict(result),
        RoleMutationStatus.Invalid => BadRequest(result),
        _ => Ok(result)
    };
}
