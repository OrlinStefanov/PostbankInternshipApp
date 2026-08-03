using BinTool.Core.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace BinTool.Infrastructure.Data;

/// <summary>
/// Applies pending migrations and seeds the Identity data that cannot be
/// expressed with HasData (users need a hashed password).
/// </summary>
public static class DbInitializer
{
    public static async Task InitializeAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var provider = scope.ServiceProvider;

        var logger = provider.GetRequiredService<ILoggerFactory>().CreateLogger(nameof(DbInitializer));
        var context = provider.GetRequiredService<AppDbContext>();

        await context.Database.MigrateAsync();

        var roleManager = provider.GetRequiredService<RoleManager<ApplicationRole>>();
        foreach (var roleName in AppRoles.All)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                await roleManager.CreateAsync(new ApplicationRole(roleName));
                logger.LogInformation("Created role {RoleName}", roleName);
            }
        }

        await SeedAdminAsync(provider, logger);
    }

    /// <summary>
    /// Creates the bootstrap admin account on an empty database.
    /// The password comes from configuration (Seed:AdminPassword, or a user secret)
    /// so no credential is committed to the repository.
    /// </summary>
    private static async Task SeedAdminAsync(IServiceProvider provider, ILogger logger)
    {
        var configuration = provider.GetRequiredService<IConfiguration>();
        var userManager = provider.GetRequiredService<UserManager<ApplicationUser>>();

        var email = configuration["Seed:AdminEmail"];
        var password = configuration["Seed:AdminPassword"];

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            logger.LogWarning(
                "Seed:AdminEmail / Seed:AdminPassword are not configured - skipping admin seeding. " +
                "Set them via user secrets or environment variables to create the first admin.");
            return;
        }

        if (await userManager.FindByEmailAsync(email) is not null)
        {
            return;
        }

        var admin = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            FullName = "System Administrator",
            IsActive = true
        };

        var result = await userManager.CreateAsync(admin, password);
        if (!result.Succeeded)
        {
            logger.LogError(
                "Failed to create the seed admin: {Errors}",
                string.Join("; ", result.Errors.Select(e => e.Description)));
            return;
        }

        await userManager.AddToRoleAsync(admin, AppRoles.Admin);
        logger.LogInformation("Created seed admin account {Email}", email);
    }
}
