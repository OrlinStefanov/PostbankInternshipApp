using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using BinTool.Api.Options;
using BinTool.Api.Services;
using BinTool.Application.Authorization;
using BinTool.Application.Models.Auth;
using BinTool.Domain.Entities;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace BinTool.Tests;

public class JwtTokenServiceTests
{
    private const string Key = "test-signing-key-that-is-long-enough-for-hmac";

    private static readonly JwtOptions Options = new()
    {
        Issuer = "BinToolTests",
        Audience = "BinToolTests",
        Key = Key,
        ExpiryMinutes = 30
    };

    private static readonly AuthenticatedUser User = new(
        "user-1", "admin", "admin@bintool.local", "System Administrator",
        Array.Empty<string>(), Array.Empty<string>());

    private static JwtTokenService CreateService() =>
        new(Microsoft.Extensions.Options.Options.Create(Options));

    private static JwtSecurityToken Parse(string token) =>
        new JwtSecurityTokenHandler().ReadJwtToken(token);

    [Fact]
    public void The_token_carries_the_user_identity()
    {
        var (token, _) = CreateService().CreateToken(User, new[] { AppRoles.Admin }, Array.Empty<string>());

        var parsed = Parse(token);

        parsed.Claims.Should().Contain(c => c.Type == ClaimTypes.NameIdentifier && c.Value == "user-1");
        parsed.Claims.Should().Contain(c => c.Type == ClaimTypes.Name && c.Value == "admin");
        parsed.Claims.Should().Contain(c => c.Type == "full_name" && c.Value == "System Administrator");
    }

    [Fact]
    public void Every_role_becomes_its_own_claim()
    {
        var (token, _) = CreateService()
            .CreateToken(User, new[] { AppRoles.Admin, AppRoles.Viewer }, Array.Empty<string>());

        Parse(token).Claims
            .Where(c => c.Type == ClaimTypes.Role)
            .Select(c => c.Value)
            .Should().BeEquivalentTo(AppRoles.Admin, AppRoles.Viewer);
    }

    [Fact]
    public void Every_permission_becomes_its_own_claim()
    {
        var (token, _) = CreateService().CreateToken(
            User, Array.Empty<string>(),
            new[] { Permissions.BinRangesRead, Permissions.BinRangesWrite });

        Parse(token).Claims
            .Where(c => c.Type == PermissionClaimTypes.Permission)
            .Select(c => c.Value)
            .Should().BeEquivalentTo(Permissions.BinRangesRead, Permissions.BinRangesWrite);
    }

    [Fact]
    public void The_expiry_follows_the_configured_lifetime()
    {
        var before = DateTime.UtcNow;

        var (_, expiresAt) = CreateService().CreateToken(User, Array.Empty<string>(), Array.Empty<string>());

        expiresAt.Should().BeCloseTo(before.AddMinutes(Options.ExpiryMinutes), TimeSpan.FromSeconds(10));
    }

    [Fact]
    public void The_issuer_and_audience_come_from_configuration()
    {
        var (token, _) = CreateService().CreateToken(User, Array.Empty<string>(), Array.Empty<string>());

        var parsed = Parse(token);

        parsed.Issuer.Should().Be("BinToolTests");
        parsed.Audiences.Should().ContainSingle().Which.Should().Be("BinToolTests");
    }

    [Fact]
    public void The_token_validates_against_the_configured_key()
    {
        var (token, _) = CreateService().CreateToken(User, new[] { AppRoles.Viewer }, Array.Empty<string>());

        var principal = new JwtSecurityTokenHandler().ValidateToken(token, new TokenValidationParameters
        {
            ValidIssuer = Options.Issuer,
            ValidAudience = Options.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Key)),
            ClockSkew = TimeSpan.Zero
        }, out _);

        principal.IsInRole(AppRoles.Viewer).Should().BeTrue();
    }

    [Fact]
    public void A_token_signed_with_another_key_is_rejected()
    {
        var (token, _) = CreateService().CreateToken(User, Array.Empty<string>(), Array.Empty<string>());

        var validate = () => new JwtSecurityTokenHandler().ValidateToken(token, new TokenValidationParameters
        {
            ValidIssuer = Options.Issuer,
            ValidAudience = Options.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes("a-completely-different-signing-key-value")),
            ClockSkew = TimeSpan.Zero
        }, out _);

        validate.Should().Throw<SecurityTokenInvalidSignatureException>();
    }
}
