using System.Reflection;
using System.Text;
using System.Text.Json.Serialization;
using BinTool.Api.Options;
using BinTool.Api.Services;
using BinTool.Core.Entities;
using BinTool.Core.Models.Import;
using BinTool.Core.Services;
using BinTool.Infrastructure.Data;
using BinTool.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
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

            // Stated rather than left to Identity's defaults, which are the same numbers
            // but invisible: a lockout looks exactly like a wrong password from outside,
            // so anyone hitting one needs to be able to read what the rule is.
            options.Lockout.MaxFailedAccessAttempts = 5;
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
            options.Lockout.AllowedForNewUsers = true;
        })
        .AddEntityFrameworkStores<AppDbContext>()
        .AddDefaultTokenProviders();

        services.AddJwtAuthentication(configuration);

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
                    "deleted). `GET /api/BinRanges/filters` returns the reference values to filter by.\n\n" +
                    "**Maintaining ranges one at a time** covers what a CSV does not: `POST`, `PUT` " +
                    "and `DELETE` on `/api/BinRanges` add, edit and withdraw a single range, and " +
                    "`POST /api/BinRanges/{id}/restore` brings a withdrawn one back. Deletes are " +
                    "soft, so nothing is erased and every change records who made it.\n\n" +
                    "**Authentication.** Every endpoint except `POST /api/Auth/login` and " +
                    "`GET /api/Health` needs a bearer token. Call login, then use the **Authorize** " +
                    "button above with the `accessToken` it returns. Reading (classify, browse) is " +
                    "open to both roles; anything that writes BIN data - importing, resolving " +
                    "conflicts, and adding, editing, deleting or restoring a range - requires **Admin**."
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

            // Lets Swagger UI's Authorize button send the token from POST /api/Auth/login.
            options.AddSecurityDefinition(JwtBearerDefaults.AuthenticationScheme, new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description =
                    "Paste the `accessToken` returned by `POST /api/Auth/login`. " +
                    "Swagger adds the `Bearer ` prefix itself."
            });

            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = JwtBearerDefaults.AuthenticationScheme
                        }
                    },
                    Array.Empty<string>()
                }
            });
        });

        services.AddEndpointsApiExplorer();

        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, HttpContextCurrentUser>();
        services.AddSingleton<IJwtTokenService, JwtTokenService>();

        services.AddScoped<IAuditLog, AuditLog>();

        services.AddScoped<IBinCsvImportService, BinCsvImportService>();
        services.AddScoped<IBinClassificationService, BinClassificationService>();
        services.AddScoped<IBinRangeQueryService, BinRangeQueryService>();
        services.AddScoped<IBinRangeAdminService, BinRangeAdminService>();

        return services;
    }

    /// <summary>
    /// Configures bearer-token authentication. Identity brings cookie schemes with it, so
    /// the defaults are set explicitly to JWT - otherwise an unauthenticated API call would
    /// be answered with a redirect to a login page that does not exist here.
    /// </summary>
    private static IServiceCollection AddJwtAuthentication(
        this IServiceCollection services, IConfiguration configuration)
    {
        var section = configuration.GetSection(JwtOptions.SectionName);
        services.Configure<JwtOptions>(section);

        var options = section.Get<JwtOptions>() ?? new JwtOptions();

        if (Encoding.UTF8.GetByteCount(options.Key) < JwtOptions.MinimumKeyBytes)
        {
            // Fail at startup rather than issue tokens that are cheap to forge.
            throw new InvalidOperationException(
                $"Jwt:Key must be configured with at least {JwtOptions.MinimumKeyBytes} bytes. " +
                "Set it via user secrets or an environment variable.");
        }

        services.AddAuthentication(auth =>
        {
            auth.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            auth.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            auth.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(jwt =>
        {
            jwt.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = options.Issuer,
                ValidAudience = options.Audience,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.Key)),

                // The default five-minute grace period would keep expired tokens working
                // well past the expiry the client was told about.
                ClockSkew = TimeSpan.Zero
            };
        });

        services.AddAuthorization();

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