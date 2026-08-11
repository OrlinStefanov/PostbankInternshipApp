using Microsoft.AspNetCore.Authorization;

namespace BinTool.Application.Authorization;

/// <summary>
/// Requires the caller to hold a named permission. One is built per endpoint policy by
/// <see cref="PermissionPolicyProvider"/>.
/// </summary>
public sealed class PermissionRequirement : IAuthorizationRequirement
{
    public PermissionRequirement(string permission) => Permission = permission;

    public string Permission { get; }
}
