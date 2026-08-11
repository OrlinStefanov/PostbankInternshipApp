using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using BinTool.Api.Options;
using BinTool.Application.Authorization;
using BinTool.Domain.Entities;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace BinTool.Api.Services;

public interface IJwtTokenService
{
    (string Token, DateTime ExpiresAtUtc) CreateToken(
        ApplicationUser user, IEnumerable<string> roles, IEnumerable<string> permissions);
}

public class JwtTokenService : IJwtTokenService
{
    private readonly JwtOptions _options;

    public JwtTokenService(IOptions<JwtOptions> options)
    {
        _options = options.Value;
    }

    public (string Token, DateTime ExpiresAtUtc) CreateToken(
        ApplicationUser user, IEnumerable<string> roles, IEnumerable<string> permissions)
    {
        var issuedAt = DateTime.UtcNow;
        var expiresAt = issuedAt.AddMinutes(_options.ExpiryMinutes);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Name, user.UserName ?? string.Empty)
        };

        if (!string.IsNullOrEmpty(user.Email))
        {
            claims.Add(new Claim(ClaimTypes.Email, user.Email));
        }

        if (!string.IsNullOrEmpty(user.FullName))
        {
            claims.Add(new Claim("full_name", user.FullName));
        }

        // One claim per role, so [Authorize(Roles = ...)] works without any extra mapping.
        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        // One claim per permission, so the permission policies can check the token directly
        // without going back to the database on every request.
        foreach (var permission in permissions)
        {
            claims.Add(new Claim(PermissionClaimTypes.Permission, permission));
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.Key));

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            notBefore: issuedAt,
            expires: expiresAt,
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));

        return (new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
    }
}
