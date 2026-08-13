using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using BinTool.Api.Errors;
using Microsoft.AspNetCore.RateLimiting;

namespace BinTool.Api.Extensions;

public static class WebApiExtensions
{
    public static IServiceCollection AddWebApi(this IServiceCollection services)
    {
        services.AddControllers().AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        });

        services.AddProblemDetails();
        services.AddExceptionHandler<ApiExceptionHandler>();

        services.AddHttpContextAccessor();
        services.AddLoginRateLimiter();

        return services;
    }

    private static IServiceCollection AddLoginRateLimiter(this IServiceCollection services) =>
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            options.AddPolicy(RateLimitPolicies.Login, context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 10,
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 0
                    }));
        });
}

public static class RateLimitPolicies
{
    public const string Login = "login";
}
