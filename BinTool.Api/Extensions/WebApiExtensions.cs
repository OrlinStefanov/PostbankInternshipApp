using System.Text.Json.Serialization;

namespace BinTool.Api.Extensions;

public static class WebApiExtensions
{
    public static IServiceCollection AddWebApi(this IServiceCollection services)
    {
        services.AddControllers().AddJsonOptions(options =>
        {
            // Enums go out as their names, matching the names the query string accepts,
            // so "Expired" means the same thing in a request and in a response.
            options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        });

        services.AddHttpContextAccessor();

        return services;
    }
}
