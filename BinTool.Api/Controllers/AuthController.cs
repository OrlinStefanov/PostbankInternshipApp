using System.Security.Claims;
using BinTool.Api.Services;
using BinTool.Core.Entities;
using BinTool.Core.Models.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace BinTool.Api.Controllers;

/// <summary>
/// Issues the access tokens the rest of the API expects.
/// <para>
/// Protected by default so anything added here is closed unless it opts out; only
/// <see cref="Login"/> is anonymous, because you cannot hold a token before you log in.
/// </para>
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[Authorize]
public class AuthController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _users;
    private readonly SignInManager<ApplicationUser> _signIn;
    private readonly IJwtTokenService _tokens;

    public AuthController(
        UserManager<ApplicationUser> users,
        SignInManager<ApplicationUser> signIn,
        IJwtTokenService tokens)
    {
        _users = users;
        _signIn = signIn;
        _tokens = tokens;
    }

    /// <summary>
    /// Exchanges credentials for an access token.
    /// </summary>
    /// <remarks>
    /// Sample request:
    ///
    ///     POST /api/auth/login
    ///     { "userName": "admin", "password": "Admin@123" }
    ///
    /// Send the returned token on every other call as
    /// `Authorization: Bearer &lt;accessToken&gt;`. It carries the user's roles, which is
    /// what the API authorizes against - a client hiding a button changes nothing here.
    ///
    /// The token expires at `expiresAtUtc`. There is no refresh token: when it expires
    /// the user logs in again.
    ///
    /// Either the user name or the email address works as `userName`. A wrong password,
    /// an unknown user and a deactivated account all return the same 401 with the same
    /// message, so the response cannot be used to discover which accounts exist.
    /// </remarks>
    /// <param name="request">The credentials.</param>
    /// <response code="200">The credentials were accepted. Returns the token and the user's roles.</response>
    /// <response code="400">The user name or password was missing.</response>
    /// <response code="401">The credentials were rejected, or the account is deactivated.</response>
    [AllowAnonymous]
    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var user = await _users.FindByNameAsync(request.UserName)
                   ?? await _users.FindByEmailAsync(request.UserName);

        // Deliberately one message for every failure: a distinct "no such user" would
        // let anyone enumerate accounts.
        if (user is null || !user.IsActive)
        {
            return Unauthorized(new { message = "Invalid user name or password." });
        }

        var check = await _signIn.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);
        if (!check.Succeeded)
        {
            return Unauthorized(new { message = "Invalid user name or password." });
        }

        var roles = await _users.GetRolesAsync(user);
        var (token, expiresAt) = _tokens.CreateToken(user, roles);

        user.LastLoginAt = DateTime.UtcNow;

        await _users.UpdateAsync(user);

        return Ok(new LoginResponse
        {
            AccessToken = token,
            ExpiresAtUtc = expiresAt,
            UserId = user.Id,
            UserName = user.UserName ?? string.Empty,
            FullName = user.FullName,
            Roles = roles.ToList()
        });
    }

    /// <summary>
    /// Returns the identity behind the bearer token.
    /// </summary>
    /// <remarks>
    /// Useful for confirming a token is still valid and seeing which roles it grants.
    /// </remarks>
    /// <response code="200">The token is valid. Returns the caller's id, name and roles.</response>
    /// <response code="401">No token was sent, or it has expired.</response>
    [Authorize]
    [HttpGet("me")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public IActionResult Me()
    {
        return Ok(new
        {
            userId = User.FindFirstValue(ClaimTypes.NameIdentifier),
            userName = User.Identity?.Name,
            fullName = User.FindFirstValue("full_name"),
            roles = User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList()
        });
    }
}
