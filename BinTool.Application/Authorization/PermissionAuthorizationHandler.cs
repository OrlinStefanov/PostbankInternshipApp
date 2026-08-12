using BinTool.Domain.Entities;
using Microsoft.AspNetCore.Authorization;

namespace BinTool.Application.Authorization;

// Admin holds every permission, including ones added to the catalog after it was seeded. Everyone
// else needs the matching Permission claim, which a login stamps on from the roles held at the time
// - so a change to a role reaches a signed-in user when their next token is issued, not mid-session.
public sealed class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context, PermissionRequirement requirement)
    {
        if (context.User.IsInRole(AppRoles.Admin) ||
            context.User.HasClaim(PermissionClaimTypes.Permission, requirement.Permission))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
