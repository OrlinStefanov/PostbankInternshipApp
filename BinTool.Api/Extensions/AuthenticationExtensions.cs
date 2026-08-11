using System.Text;
using BinTool.Api.Options;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace BinTool.Api.Extensions;

public static class AuthenticationExtensions
{
    /// <summary>
    /// Configures bearer-token authentication. Identity brings cookie schemes with it, so
    /// the defaults are set explicitly to JWT - otherwise an unauthenticated API call would
    /// be answered with a redirect to a login page that does not exist here.
    /// </summary>
    public static IServiceCollection AddJwtAuthentication(
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

                ClockSkew = TimeSpan.Zero
            };
        });

        services.AddPermissionAuthorization();

        return services;
    }
}
