using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace BinTool.Core.Authorization;

public static class PermissionAuthorizationExtensions
{
    /// <summary>
    /// Registers permission-based authorization: the on-demand <see cref="PermissionPolicyProvider"/>
    /// and the <see cref="PermissionAuthorizationHandler"/> that treats Admin as a superuser.
    /// Called by both the API and the UI so the two enforce permissions identically.
    /// </summary>
    public static IServiceCollection AddPermissionAuthorization(this IServiceCollection services)
    {
        services.AddAuthorizationCore();
        services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
        services.AddSingleton<IAuthorizationHandler, PermissionAuthorizationHandler>();

        return services;
    }
}
