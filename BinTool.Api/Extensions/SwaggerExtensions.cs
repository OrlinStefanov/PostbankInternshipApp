using System.Reflection;
using BinTool.Api.Documentation;
using BinTool.Application.Models.Import;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.OpenApi.Models;

namespace BinTool.Api.Extensions;

public static class SwaggerExtensions
{
    private const string DocumentName = "v1";

    private const string DescriptionResource = "BinTool.Api.Documentation.ApiDescription.md";

    public static IServiceCollection AddSwaggerDocumentation(this IServiceCollection services)
    {
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc(DocumentName, new OpenApiInfo
            {
                Title = "BinTool API",
                Version = DocumentName,
                Description = ReadDescription()
            });

            IncludeXmlComments(options);

            // After the XML comments, so the markdown is what ends up on Description.
            options.OperationFilter<OperationRemarksFilter>();

            options.SupportNonNullableReferenceTypes();
            AddBearerSecurity(options);
        });

        services.AddEndpointsApiExplorer();

        return services;
    }

    private static string ReadDescription()
    {
        using var stream = Assembly.GetExecutingAssembly()
            .GetManifestResourceStream(DescriptionResource);

        if (stream is null) return string.Empty;

        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    private static void IncludeXmlComments(Swashbuckle.AspNetCore.SwaggerGen.SwaggerGenOptions options)
    {
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
    }

    private static void AddBearerSecurity(Swashbuckle.AspNetCore.SwaggerGen.SwaggerGenOptions options)
    {
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
    }
}
