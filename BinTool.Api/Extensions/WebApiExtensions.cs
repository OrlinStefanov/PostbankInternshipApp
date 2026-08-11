using System.Text.Json.Serialization;
using BinTool.Api.Errors;

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

        // ProblemDetails is what [ApiController] already returns for a failed model binding,
        // so an error the pipeline raises now has the same shape as one an action produced.
        services.AddProblemDetails();
        services.AddExceptionHandler<ApiExceptionHandler>();

        services.AddHttpContextAccessor();

        return services;
    }
}
