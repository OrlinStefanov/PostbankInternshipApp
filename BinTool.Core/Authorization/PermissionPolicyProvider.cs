using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace BinTool.Core.Authorization;

/// <summary>
/// Supplies an authorization policy on demand for any catalog permission, so an endpoint can
/// write <c>[Authorize(Policy = Permissions.BinRangesWrite)]</c> without every policy having to
/// be registered up front. A policy name that is not a known permission falls through to the
/// default provider, so ordinary named policies still work.
/// </summary>
public sealed class PermissionPolicyProvider : IAuthorizationPolicyProvider
{
    private readonly DefaultAuthorizationPolicyProvider _fallback;

    public PermissionPolicyProvider(IOptions<AuthorizationOptions> options)
    {
        _fallback = new DefaultAuthorizationPolicyProvider(options);
    }

    public Task<AuthorizationPolicy> GetDefaultPolicyAsync() => _fallback.GetDefaultPolicyAsync();

    public Task<AuthorizationPolicy?> GetFallbackPolicyAsync() => _fallback.GetFallbackPolicyAsync();

    public Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        if (Permissions.IsPermission(policyName))
        {
            var policy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .AddRequirements(new PermissionRequirement(policyName))
                .Build();

            return Task.FromResult<AuthorizationPolicy?>(policy);
        }

        return _fallback.GetPolicyAsync(policyName);
    }
}
