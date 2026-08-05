using System.Security.Claims;
using BinTool.Core.Authorization;
using BinTool.Core.Entities;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace BinTool.Tests;

/// <summary>
/// The two pieces that turn a permission key into an enforced rule: the handler (which makes
/// Admin a superuser) and the on-demand policy provider.
/// </summary>
public class PermissionAuthorizationTests
{
    private static ClaimsPrincipal Principal(string[]? roles = null, string[]? permissions = null)
    {
        var claims = new List<Claim>();
        foreach (var role in roles ?? Array.Empty<string>())
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }
        foreach (var permission in permissions ?? Array.Empty<string>())
        {
            claims.Add(new Claim(PermissionClaimTypes.Permission, permission));
        }

        var identity = new ClaimsIdentity(claims, "test", ClaimTypes.Name, ClaimTypes.Role);
        return new ClaimsPrincipal(identity);
    }

    private static async Task<bool> Allows(ClaimsPrincipal user, string permission)
    {
        var requirement = new PermissionRequirement(permission);
        var context = new AuthorizationHandlerContext(new[] { requirement }, user, resource: null);

        await new PermissionAuthorizationHandler().HandleAsync(context);

        return context.HasSucceeded;
    }

    [Fact]
    public async Task Admin_passes_any_permission_even_one_not_granted()
    {
        var admin = Principal(roles: new[] { AppRoles.Admin });

        (await Allows(admin, Permissions.BinRangesWrite)).Should().BeTrue();
        (await Allows(admin, Permissions.RolesManage)).Should().BeTrue();
    }

    [Fact]
    public async Task A_granted_permission_passes()
    {
        var user = Principal(permissions: new[] { Permissions.BinRangesRead });

        (await Allows(user, Permissions.BinRangesRead)).Should().BeTrue();
    }

    [Fact]
    public async Task A_permission_the_user_lacks_fails()
    {
        var user = Principal(permissions: new[] { Permissions.BinRangesRead });

        (await Allows(user, Permissions.BinRangesWrite)).Should().BeFalse();
    }

    [Fact]
    public async Task The_policy_provider_builds_a_requirement_for_a_catalog_permission()
    {
        var provider = new PermissionPolicyProvider(Options.Create(new AuthorizationOptions()));

        var policy = await provider.GetPolicyAsync(Permissions.BinRangesWrite);

        policy.Should().NotBeNull();
        policy!.Requirements.OfType<PermissionRequirement>()
            .Should().ContainSingle(r => r.Permission == Permissions.BinRangesWrite);
    }

    [Fact]
    public async Task The_policy_provider_ignores_a_name_that_is_not_a_permission()
    {
        var provider = new PermissionPolicyProvider(Options.Create(new AuthorizationOptions()));

        (await provider.GetPolicyAsync("not-a-permission")).Should().BeNull();
    }
}
