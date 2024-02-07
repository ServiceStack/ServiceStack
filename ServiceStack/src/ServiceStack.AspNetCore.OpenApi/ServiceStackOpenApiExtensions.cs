using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Any;
using ServiceStack.AspNetCore.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace ServiceStack;

public static class ServiceStackOpenApiExtensions
{
    public static OpenApiMetadata OpenApiMetadata { get; set; } = new();

    public static void WithOpenApi(this ServiceStackOptions options)
    {
        if (!options.MapEndpointRouting)
            throw new NotSupportedException("MapEndpointRouting must be enabled to use OpenApi");

        options.RouteHandlerBuilders.Add((builder, operation, method, route) =>
        {
            builder.WithOpenApi(op =>
            {
                OpenApiMetadata.AddOperation(op, operation, method, route);
                return op;
            });
        });
    }

    public static void AddSwagger(this ServiceStackServicesOptions options, Action<OpenApiMetadata>? configure = null)
    {
        configure?.Invoke(OpenApiMetadata);

        options.Services!.AddSingleton(OpenApiMetadata);
        options.Services!.AddSingleton<IConfigureOptions<SwaggerGenOptions>, ConfigureServiceStackSwagger>();
        options.Services!.AddSingleton<IConfigureOptions<ServiceStackOptions>, ConfigureServiceStackSwagger>();
    }

    public static void AddServiceStackSwagger(this IServiceCollection services, Action<OpenApiMetadata>? configure = null)
    {
        configure?.Invoke(OpenApiMetadata);

        services.AddSingleton(OpenApiMetadata);
        services.AddSingleton<IConfigureOptions<SwaggerGenOptions>, ConfigureServiceStackSwagger>();
        services.AddSingleton<IConfigureOptions<ServiceStackOptions>, ConfigureServiceStackSwagger>();
    }

    public static void AddBasicAuth(this SwaggerGenOptions options) =>
        options.AddSecurityDefinition(OpenApiSecurity.BasicAuthScheme.Scheme, OpenApiSecurity.BasicAuthScheme);

    public static void AddJwtAuth(this SwaggerGenOptions options) =>
        options.AddSecurityDefinition(OpenApiSecurity.JwtBearerScheme.Scheme, OpenApiSecurity.JwtBearerScheme);

    internal static List<IOpenApiAny> ToOpenApiEnums(this IEnumerable<string>? enums) =>
        enums.Safe().Map(x => (IOpenApiAny)new OpenApiString(x));
}