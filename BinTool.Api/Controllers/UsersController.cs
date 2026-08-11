using BinTool.Api.Errors;
using BinTool.Application.Abstractions;
using BinTool.Application.Authorization;
using BinTool.Application.Models.Access;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BinTool.Api.Controllers;

/// <summary>
/// Lists users and assigns roles to them. This is where a custom role becomes real for a
/// person.
/// <para>
/// Requires the <c>roles.manage</c> permission. It does not create accounts or change passwords.
/// Two rules protect the god account: the last admin keeps Admin, and no one strips their own.
/// </para>
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[Authorize(Policy = Permissions.RolesManage)]
public class UsersController : ControllerBase
{
    private readonly IUserAdminService _service;

    public UsersController(IUserAdminService service)
    {
        _service = service;
    }

    /// <summary>Lists users with the roles they currently hold.</summary>
    /// <response code="200">The users.</response>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<UserListItem>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUsers(CancellationToken cancellationToken)
    {
        return Ok(await _service.GetUsersAsync(cancellationToken));
    }

    /// <summary>Replaces a user's roles with the supplied set.</summary>
    /// <param name="id">Id of the user.</param>
    /// <param name="input">The complete set of roles the user should hold.</param>
    /// <response code="200">Updated. The body carries the user with their roles.</response>
    /// <response code="400">A named role does not exist.</response>
    /// <response code="404">No user has that id.</response>
    /// <response code="409">The change would remove the last admin, or your own Admin role.</response>
    [HttpPut("{id}/roles")]
    [ProducesResponseType(typeof(UserRolesResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(UserRolesResult), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(UserRolesResult), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(UserRolesResult), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> SetRoles(
        string id, [FromBody] UserRolesInput input, CancellationToken cancellationToken)
    {
        return ApiResults.From(await _service.SetRolesAsync(id, input, cancellationToken));
    }
}
