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
    /// <summary>
    /// The demo accounts created on an empty database so the application can be signed
    /// into straight after a clone. Each can be overridden from configuration, and the
    /// whole step is skipped when <c>Seed:DemoUsers</c> is false.
    /// </summary>
    private static readonly SeedUser[] DemoUsers =
    {
        new("admin", "admin@bintool.local", "Admin@123", "System Administrator", AppRoles.Admin),
        new("viewer", "viewer@bintool.local", "Viewer@123", "Read-only User", AppRoles.Viewer)
    };

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

        await SeedUsersAsync(provider, logger);
    }

    /// <summary>
    /// Creates the demo accounts on an empty database. Existing accounts are left alone,
    /// so a changed password is never reset by a restart.
    /// </summary>
    private static async Task SeedUsersAsync(IServiceProvider provider, ILogger logger)
    {
        var configuration = provider.GetRequiredService<IConfiguration>();
        var userManager = provider.GetRequiredService<UserManager<ApplicationUser>>();

        // Unset means enabled, so a fresh clone can be signed into without configuration.
        if (bool.TryParse(configuration["Seed:DemoUsers"], out var enabled) && !enabled)
        {
            logger.LogInformation("Seed:DemoUsers is false - skipping demo account seeding.");
            return;
        }

        foreach (var seed in DemoUsers)
        {
            // Per-role overrides, so a real deployment can supply its own credentials
            // without editing code: Seed:Admin:UserName, Seed:Admin:Password, and so on.
            var section = configuration.GetSection($"Seed:{seed.Role}");
            var userName = section["UserName"] ?? seed.UserName;
            var email = section["Email"] ?? seed.Email;
            var password = section["Password"] ?? seed.Password;
            var fullName = section["FullName"] ?? seed.FullName;

            if (await userManager.FindByNameAsync(userName) is not null)
            {
                continue;
            }

            var user = new ApplicationUser
            {
                UserName = userName,
                Email = email,
                EmailConfirmed = true,
                FullName = fullName,
                IsActive = true
            };

            var result = await userManager.CreateAsync(user, password);
            if (!result.Succeeded)
            {
                logger.LogError(
                    "Failed to create the seed {Role} account: {Errors}",
                    seed.Role, string.Join("; ", result.Errors.Select(e => e.Description)));
                continue;
            }

            await userManager.AddToRoleAsync(user, seed.Role);
            logger.LogInformation(
                "Created seed {Role} account {UserName}", seed.Role, userName);
        }
    }

    private sealed record SeedUser(
        string UserName, string Email, string Password, string FullName, string Role);
}
