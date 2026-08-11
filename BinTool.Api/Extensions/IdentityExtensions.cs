using BinTool.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;

namespace BinTool.Api.Extensions;

public static class IdentityExtensions
{
    public static IServiceCollection AddIdentityServices(this IServiceCollection services)
    {
        services.AddIdentity<ApplicationUser, ApplicationRole>(options =>
        {
            options.Password.RequireDigit = true;
            options.Password.RequiredLength = 8;
            options.Password.RequireNonAlphanumeric = true;
            options.SignIn.RequireConfirmedEmail = false;

            // Stated rather than left to Identity's defaults, which are the same numbers
            // but invisible: a lockout looks exactly like a wrong password from outside,
            // so anyone hitting one needs to be able to read what the rule is.
            options.Lockout.MaxFailedAccessAttempts = 5;
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
            options.Lockout.AllowedForNewUsers = true;
        })
        .AddEntityFrameworkStores<AppDbContext>()
        .AddDefaultTokenProviders();

        return services;
    }
}
