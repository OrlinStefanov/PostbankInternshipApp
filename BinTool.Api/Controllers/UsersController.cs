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
public class UsersController : ControllerBase
{
    private readonly IUserAdminService _service;

    public UsersController(IUserAdminService service)
    {
        _service = service;
    }

    /// <summary>Lists users with the roles they currently hold.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<UserListItem>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUsers(CancellationToken cancellationToken)
    {
        return Ok(await _service.GetUsersAsync(cancellationToken));
    }

    /// <summary>Replaces a user's roles with the supplied set.</summary>
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
