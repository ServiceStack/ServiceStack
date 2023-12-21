using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using ServiceStack.AspNetCore.OpenApi;
using ServiceStack.Auth;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace ServiceStack;

public static class ServiceStackOpenApiExtensions
{
    public static OpenApiMetadata OpenApiMetadata { get; set; } = new();

    public static void WithOpenApi(this ServiceStackOptions options, bool useEndpointRoutes = true)
    {
        options.MapEndpointRouting = true;
        options.UseEndpointRouting = useEndpointRoutes;
        options.RouteHandlerBuilders.Add((builder, operation, method, route) =>
        {
            builder.WithOpenApi(op =>
            {
                OpenApiMetadata.AddOperation(op, operation, method, route);
                return op;
            });
        });
    }
    
    public static void AddServiceStack(this SwaggerGenOptions swaggerGenOptions, Action<OpenApiMetadata>? configure = null)
    {
        configure?.Invoke(OpenApiMetadata);

        foreach (var filterType in OpenApiMetadata.DocumentFilterTypes)
        {
            swaggerGenOptions.DocumentFilterDescriptors.Add(new FilterDescriptor
            {
                Type = filterType,
                Arguments = Array.Empty<object>(),
            });
        }
        foreach (var filterType in OpenApiMetadata.SchemaFilterTypes)
        {
            swaggerGenOptions.SchemaFilterDescriptors.Add(new FilterDescriptor
            {
                Type = filterType,
                Arguments = Array.Empty<object>(),
            });
        }
    }

    public static void AddBasicAuth(this SwaggerGenOptions swaggerGenOptions, string scheme)
    {
        swaggerGenOptions.AddSecurityDefinition(BasicAuthenticationHandler.Scheme, new OpenApiSecurityScheme
        {
            Name = "Authorization",
            Type = SecuritySchemeType.Http,
            Scheme = scheme,
            In = ParameterLocation.Header,
            Description = "HTTP Basic access authentication"
        });
        swaggerGenOptions.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = scheme
                    }
                },
                ["Basic "]
            }
        });
    }

    internal static List<IOpenApiAny> ToOpenApiEnums(this IEnumerable<string>? enums) => 
        enums.Safe().Map(x => (IOpenApiAny) new OpenApiString(x));
}
