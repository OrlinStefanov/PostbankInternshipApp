using System.Security.Claims;
using BinTool.Api.Services;
using BinTool.Application.Mapping;
using BinTool.Application.Models.Auth;
using BinTool.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BinTool.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[Authorize]
public class AuthController : ControllerBase
{
    private readonly ISignInService _signIn;
    private readonly IJwtTokenService _tokens;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        ISignInService signIn,
        IJwtTokenService tokens,
        ILogger<AuthController> logger)
    {
        _signIn = signIn;
        _tokens = tokens;
        _logger = logger;
    }

    /// <summary>Exchanges credentials for an access token.</summary>
    [AllowAnonymous]
    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(LoginRejection), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login(
        [FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        var outcome = await _signIn.SignInAsync(
            request.UserName, request.Password, cancellationToken);

        if (outcome.User is null)
        {
            return Unauthorized(SignInMapper.ToRejection(outcome));
        }

        var (token, expiresAt) = _tokens.CreateToken(
            outcome.User, outcome.User.Roles, outcome.User.Permissions);

        // The token itself is never logged - only that one was issued and when it lapses.
        SignInLog.SignedIn(_logger, outcome.User.UserName, expiresAt);

        return Ok(SignInMapper.ToResponse(outcome.User, token, expiresAt));
    }

    /// <summary>Returns the identity behind the bearer token.</summary>
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
