using BinTool.Domain.Entities;
using Microsoft.AspNetCore.Authorization;

namespace BinTool.Application.Authorization;

// Grants a PermissionRequirement when the caller either is in the Admin role - a superuser that
// holds every permission, including ones added to the catalog after it was seeded, which is what
// makes Admin a god the rest of the system cannot lock out - or carries the matching Permission
// claim, which a login stamps onto the token from the roles the user held at the time.
//
// Because permissions ride on the token, a change to a role's permissions reaches a signed-in
// user when their token is next issued, not mid-session.
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
