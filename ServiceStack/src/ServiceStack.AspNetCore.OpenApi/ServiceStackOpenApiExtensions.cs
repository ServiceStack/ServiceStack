using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Any;
using ServiceStack.AspNetCore.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace ServiceStack;

public static class ServiceStackOpenApiExtensions
{
    public static void WithOpenApi(this ServiceStackOptions options)
    {
        if (!options.MapEndpointRouting)
            throw new NotSupportedException("MapEndpointRouting must be enabled to use OpenApi");

        options.RouteHandlerBuilders.Add((builder, operation, method, route) =>
        {
            builder.WithOpenApi(op =>
            {
                OpenApiMetadata.Instance.AddOperation(op, operation, method, route);
                return op;
            });
        });
    }

    public static void AddSwagger(this ServiceStackServicesOptions options, Action<OpenApiMetadata>? configure = null)
    {
        configure?.Invoke(OpenApiMetadata.Instance);

        options.Services!.AddSingleton(OpenApiMetadata.Instance);
        options.Services!.AddSingleton<IConfigureOptions<SwaggerGenOptions>, ConfigureServiceStackSwagger>();
        options.Services!.AddSingleton<IConfigureOptions<ServiceStackOptions>, ConfigureServiceStackSwagger>();
        
        options.Services!.ConfigurePlugin<MetadataFeature>(feature => {
            feature.AddPluginLink("/swagger/index.html", "Swagger UI");
        });
    }

    public static void AddServiceStackSwagger(this IServiceCollection services, Action<OpenApiMetadata>? configure = null)
    {
        configure?.Invoke(OpenApiMetadata.Instance);

        services.AddSingleton(OpenApiMetadata.Instance);
        services.AddSingleton<IConfigureOptions<SwaggerGenOptions>, ConfigureServiceStackSwagger>();
        services.AddSingleton<IConfigureOptions<ServiceStackOptions>, ConfigureServiceStackSwagger>();
        
        services.ConfigurePlugin<MetadataFeature>(feature => {
            feature.AddPluginLink("/swagger/index.html", "Swagger UI");
        });
    }
        
    public static AuthenticationBuilder AddBasicAuth<TUser>(this IServiceCollection services)
        where TUser : IdentityUser, new() {
        OpenApiMetadata.Instance.AddBasicAuth();
        return new AuthenticationBuilder(services).AddBasicAuth<TUser,string>();
    }

    public static void AddJwtAuth(this IServiceCollection services) {
        OpenApiMetadata.Instance.AddJwtBearer();
    }

    public static void AddBasicAuth(this SwaggerGenOptions options) =>
        options.AddSecurityDefinition(OpenApiSecurity.BasicAuthScheme.Scheme, OpenApiSecurity.BasicAuthScheme);

    public static void AddJwtAuth(this SwaggerGenOptions options) =>
        options.AddSecurityDefinition(OpenApiSecurity.JwtBearerScheme.Scheme, OpenApiSecurity.JwtBearerScheme);

    public static void AddApiKeys(this SwaggerGenOptions options) =>
        options.AddSecurityDefinition(OpenApiSecurity.ApiKeyScheme.Scheme, OpenApiSecurity.ApiKeyScheme);

    internal static List<IOpenApiAny> ToOpenApiEnums(this IEnumerable<string>? enums) =>
        enums.Safe().Map(x => (IOpenApiAny)new OpenApiString(x));
}