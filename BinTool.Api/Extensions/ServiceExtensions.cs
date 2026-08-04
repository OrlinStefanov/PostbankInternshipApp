using System.Reflection;
using System.Text.Json.Serialization;
using BinTool.Core.Entities;
using BinTool.Core.Models.Import;
using BinTool.Core.Services;
using BinTool.Infrastructure.Data;
using BinTool.Infrastructure.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;

namespace BinTool.Api.Extensions;

public static class ServiceExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Database
        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlite(configuration.GetConnectionString("DefaultConnection") ?? "Data Source=bintool.db"));

        // Identity
        services.AddIdentity<ApplicationUser, ApplicationRole>(options =>
        {
            options.Password.RequireDigit = true;
            options.Password.RequiredLength = 8;
            options.Password.RequireNonAlphanumeric = true;
            options.SignIn.RequireConfirmedEmail = false;
        })
        .AddEntityFrameworkStores<AppDbContext>()
        .AddDefaultTokenProviders();

        // API
        services.AddControllers().AddJsonOptions(options =>
        {
            // Enums go out as their names, matching the names the query string accepts,
            // so "Expired" means the same thing in a request and in a response.
            options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        });
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "BinTool API",
                Version = "v1",
                Description =
                    "Card BIN classification and commission configuration.\n\n" +
                    "**Importing BIN ranges** is a two-step workflow:\n\n" +
                    "1. `POST /api/BinCsvImport/import` uploads a CSV. Rows are sorted into four " +
                    "outcomes: new prefixes are inserted, invalid rows are recorded as rejected, " +
                    "rows identical to the stored record are skipped, and rows whose prefix already " +
                    "exists with *different* values are staged as conflicts rather than overwriting " +
                    "anything.\n" +
                    "2. `POST /api/BinCsvImport/resolve-conflicts` applies a decision to each staged " +
                    "conflict - overwrite the stored record, or keep it and discard the imported row.\n\n" +
                    "Conflicts are persisted, so `GET /api/BinCsvImport/conflicts` can rebuild the " +
                    "outstanding worklist at any time (for example after the client reloads).\n\n" +
                    "**Classifying a BIN** with `POST /api/Bin/classify` matches it against the " +
                    "imported ranges, longest prefix first, and returns the card scheme, product " +
                    "type, funding type, issuing country and region. No scheme ranges are " +
                    "hard-coded - every answer comes from data in the database.\n\n" +
                    "**Browsing what is stored** is `GET /api/BinRanges`, which filters and pages " +
                    "the BIN ranges and reports each one's status (active, scheduled, expired or " +
                    "deleted). `GET /api/BinRanges/filters` returns the reference values to filter by."
            });

            // Surface the doc comments from the controllers and the shared import models.
            foreach (var assembly in new[]
                     {
                         Assembly.GetExecutingAssembly(),
                         typeof(BinImportResult).Assembly
                     })
            {
                var xml = Path.Combine(AppContext.BaseDirectory, $"{assembly.GetName().Name}.xml");
                if (File.Exists(xml))
                {
                    options.IncludeXmlComments(xml);
                }
            }

            options.SupportNonNullableReferenceTypes();
        });

        services.AddEndpointsApiExplorer();
        services.AddScoped<IBinCsvImportService, BinCsvImportService>();
        services.AddScoped<IBinClassificationService, BinClassificationService>();
        services.AddScoped<IBinRangeQueryService, BinRangeQueryService>();

        return services;
    }

    public static WebApplication AddApplicationMiddleware(this WebApplication app)
    {
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI(options =>
            {
                options.SwaggerEndpoint("/swagger/v1/swagger.json", "BinTool API v1");
            });
        }

        app.UseHttpsRedirection();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllers();

        return app;
    }
}
