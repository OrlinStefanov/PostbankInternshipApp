using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace BinTool.Application.Authorization;

public static class PermissionAuthorizationExtensions
{
    // Registers permission-based authorization: the on-demand PermissionPolicyProvider and the
    // PermissionAuthorizationHandler that treats Admin as a superuser. Called by both the API and
    // the UI so the two enforce permissions identically.
    public static IServiceCollection AddPermissionAuthorization(this IServiceCollection services)
    {
        services.AddAuthorizationCore();
        services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
        services.AddSingleton<IAuthorizationHandler, PermissionAuthorizationHandler>();

        return services;
    }
}
