using BinTool.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BinTool.Api.Extensions;

public static class PersistenceExtensions
{
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
