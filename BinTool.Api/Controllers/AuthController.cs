using System.Security.Claims;
using BinTool.Api.Services;
using BinTool.Application.Authorization;
using BinTool.Domain.Entities;
using BinTool.Application.Models.Auth;
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
    private readonly RoleManager<ApplicationRole> _roles;
    private readonly IJwtTokenService _tokens;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        UserManager<ApplicationUser> users,
        RoleManager<ApplicationRole> roles,
        IJwtTokenService tokens,
        ILogger<AuthController> logger)
    {
        _users = users;
        _roles = roles;
        _tokens = tokens;
        _logger = logger;
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
    /// Either the user name or the email address works as `userName`.
    ///
    /// A wrong password, an unknown user and a deactivated account all return the same 401
    /// with <c>Reason: InvalidCredentials</c>, so the response cannot be used to discover
    /// which accounts exist.
    ///
    /// **Five failed attempts lock the account for five minutes.** A locked-out account is
    /// reported distinctly as <c>Reason: LockedOut</c> with <c>lockoutEndsUtc</c>, because a
    /// lockout is the one refusal the caller can neither see nor fix by retrying. The
    /// password is still checked first, so **the correct password releases the lockout and
    /// signs in immediately** - it clears the failed-attempt count and the lock rather than
    /// making the owner wait it out.
    /// </remarks>
    /// <param name="request">The credentials.</param>
    /// <response code="200">The credentials were accepted. Returns the token and the user's roles.</response>
    /// <response code="400">The user name or password was missing.</response>
    /// <response code="401">The credentials were rejected, or the account is locked out.
    /// The body is a <see cref="LoginRejection"/> whose <c>reason</c> distinguishes the two.</response>
    [AllowAnonymous]
    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(LoginRejection), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var user = await _users.FindByNameAsync(request.UserName)
                   ?? await _users.FindByEmailAsync(request.UserName);

        if (user is null || !user.IsActive)
        {
            // Deliberately the same event as a wrong password: the log must not become the
            // place where "does this account exist?" can be answered.
            _logger.CredentialsRejected(request.UserName);

            return Unauthorized(new LoginRejection { Reason = LoginRejectionReason.InvalidCredentials });
        }

        // Check the password first, before the lockout - so the account's real owner is
        // never shut out by their own typos. CheckPasswordAsync only verifies the hash; it
        // does not touch the failed-attempt count or the lock.
        if (await _users.CheckPasswordAsync(user, request.Password))
        {
            // The correct password releases any lock and wipes the failed-attempt count,
            // rather than making the owner wait the window out.
            if (await _users.GetLockoutEndDateAsync(user) is not null)
            {
                await _users.SetLockoutEndDateAsync(user, null);
            }

            await _users.ResetAccessFailedCountAsync(user);
        }
        else
        {
            // A wrong password is recorded; the fifth in a row sets the lock.
            await _users.AccessFailedAsync(user);

            if (await _users.IsLockedOutAsync(user))
            {
                var lockoutEnd = await _users.GetLockoutEndDateAsync(user);

                _logger.LockedOut(user.UserName, lockoutEnd);

                return Unauthorized(new LoginRejection
                {
                    Reason = LoginRejectionReason.LockedOut,
                    LockoutEndsUtc = lockoutEnd?.UtcDateTime
                });
            }

            _logger.CredentialsRejected(request.UserName);

            return Unauthorized(new LoginRejection { Reason = LoginRejectionReason.InvalidCredentials });
        }

        var roles = await _users.GetRolesAsync(user);
        var permissions = await ResolvePermissionsAsync(roles);
        var (token, expiresAt) = _tokens.CreateToken(user, roles, permissions);

        user.LastLoginAt = DateTime.UtcNow;

        await _users.UpdateAsync(user);

        // The token itself is never logged - only that one was issued and when it lapses.
        _logger.SignedIn(user.UserName!, expiresAt);

        return Ok(new LoginResponse
        {
            AccessToken = token,
            ExpiresAtUtc = expiresAt,
            UserId = user.Id,
            UserName = user.UserName ?? string.Empty,
            FullName = user.FullName,
            Roles = roles.ToList(),
            Permissions = permissions
        });
    }

    /// <summary>
    /// Flattens the user's roles into the permissions they grant, read from each role's
    /// claims. Admin is a superuser, so it is credited with the whole catalog whether or not
    /// every grant is present - the token then advertises the same access the API enforces.
    /// </summary>
    private async Task<List<string>> ResolvePermissionsAsync(IEnumerable<string> roleNames)
    {
        var permissions = new HashSet<string>(StringComparer.Ordinal);

        foreach (var roleName in roleNames)
        {
            if (string.Equals(roleName, AppRoles.Admin, StringComparison.Ordinal))
            {
                permissions.UnionWith(Permissions.AllKeys);
                continue;
            }

            var role = await _roles.FindByNameAsync(roleName);
            if (role is null)
            {
                continue;
            }

            foreach (var claim in await _roles.GetClaimsAsync(role))
            {
                if (claim.Type == PermissionClaimTypes.Permission)
                {
                    permissions.Add(claim.Value);
                }
            }
        }

        return permissions.ToList();
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