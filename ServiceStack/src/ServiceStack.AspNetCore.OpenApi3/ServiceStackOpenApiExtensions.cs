using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using ServiceStack.AspNetCore.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace ServiceStack;

public static class ServiceStackOpenApiExtensions
{
    public static void WithOpenApi(this ServiceStackOptions options)
    {
        // In the OpenApi3 package we no longer require ASP.NET Core Endpoint Routing
        // for OpenAPI document generation. ServiceStack operations are wired into
        // the OpenAPI document via ServiceStackDocumentFilter + OpenApiMetadata
        // using Swashbuckle, based on ServiceStack's own metadata.
        //
        // This hook is kept for symmetry with the OpenApi (v1/v2) package and as
        // a future extension point if additional ServiceStackOptions-based
        // configuration is needed, but it's currently a no-op.
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

    internal static List<JsonNode> ToOpenApiEnums(this IEnumerable<string>? enums) =>
        enums.Safe().Map(x => (JsonNode)JsonValue.Create(x));
}
