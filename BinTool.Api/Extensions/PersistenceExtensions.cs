using BinTool.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BinTool.Api.Extensions;

public static class PersistenceExtensions
{
    /// <summary>
    /// Registers the database context. The fallback connection string keeps a developer
    /// clone runnable with no configuration; anything else supplies its own.
    /// </summary>
    public static IServiceCollection AddPersistence(
        this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlite(
                configuration.GetConnectionString("DefaultConnection")
                ?? "Data Source=bintool.db"));

        return services;
    }
}
