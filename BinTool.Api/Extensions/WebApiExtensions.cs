using System.Text.Json.Serialization;
using BinTool.Api.Errors;

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

        return services;
    }
}
