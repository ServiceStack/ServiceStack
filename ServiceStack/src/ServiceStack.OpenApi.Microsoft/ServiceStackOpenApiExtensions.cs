using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using ServiceStack.AspNetCore.OpenApi;

namespace ServiceStack;

public static class ServiceStackOpenApiExtensions
{
    public static void WithOpenApi(this ServiceStackOptions options)
    {
        // ServiceStack operations are wired into the OpenAPI document via
        // ServiceStackDocumentTransformer + OpenApiMetadata using Microsoft.AspNetCore.OpenApi,
        // based on ServiceStack's own metadata.
        //
        // This hook is kept for symmetry with other OpenApi packages and as
        // a future extension point if additional ServiceStackOptions-based
        // configuration is needed, but it's currently a no-op.
    }

    public static void AddOpenApi(this ServiceStackServicesOptions options, Action<OpenApiMetadata>? configure = null)
    {
        configure?.Invoke(OpenApiMetadata.Instance);

        options.Services!.AddSingleton(OpenApiMetadata.Instance);
        options.Services!.AddSingleton<IConfigureOptions<OpenApiOptions>, ConfigureServiceStackOpenApi>();
        options.Services!.AddSingleton<IConfigureOptions<ServiceStackOptions>, ConfigureServiceStackOpenApi>();

        options.Services!.ConfigurePlugin<MetadataFeature>(feature => {
            feature.AddPluginLink("/openapi/v1.json", "OpenAPI");
        });
    }

    public static void AddServiceStackOpenApi(this IServiceCollection services, string documentName = "v1", Action<OpenApiMetadata>? configure = null)
    {
        configure?.Invoke(OpenApiMetadata.Instance);

        services.AddSingleton(OpenApiMetadata.Instance);

        // Register the transformer types as services so they can be DI-activated
        foreach (var transformerType in OpenApiMetadata.Instance.DocumentTransformerTypes)
        {
            services.AddTransient(transformerType);
        }
        foreach (var transformerType in OpenApiMetadata.Instance.SchemaTransformerTypes)
        {
            services.AddTransient(transformerType);
        }

        // Configure OpenApiOptions for the specific document name
        // Use PostConfigure to ensure this runs after AddOpenApi()
        services.PostConfigure<OpenApiOptions>(documentName, options =>
        {
            // Register document transformers using DI activation
            foreach (var transformerType in OpenApiMetadata.Instance.DocumentTransformerTypes)
            {
                // Use reflection to call AddDocumentTransformer<T>()
                var method = typeof(OpenApiOptions).GetMethod(nameof(OpenApiOptions.AddDocumentTransformer),
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance,
                    null, Type.EmptyTypes, null);
                var genericMethod = method!.MakeGenericMethod(transformerType);
                genericMethod.Invoke(options, null);
            }

            // Register schema transformers using DI activation
            foreach (var transformerType in OpenApiMetadata.Instance.SchemaTransformerTypes)
            {
                // Use reflection to call AddSchemaTransformer<T>()
                var method = typeof(OpenApiOptions).GetMethod(nameof(OpenApiOptions.AddSchemaTransformer),
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance,
                    null, Type.EmptyTypes, null);
                var genericMethod = method!.MakeGenericMethod(transformerType);
                genericMethod.Invoke(options, null);
            }
        });

        services.AddSingleton<IConfigureOptions<ServiceStackOptions>, ConfigureServiceStackOpenApi>();

        services.ConfigurePlugin<MetadataFeature>(feature => {
            feature.AddPluginLink($"/openapi/{documentName}.json", "OpenAPI");
        });
    }
        
    public static AuthenticationBuilder AddBasicAuth<TUser>(this IServiceCollection services)
        where TUser : IdentityUser, new() {
        OpenApiMetadata.Instance.AddBasicAuth();
        return new AuthenticationBuilder(services).AddBasicAuth<TUser,string>();
    }

    public static IServiceCollection AddJwtAuth(this IServiceCollection services) {
        OpenApiMetadata.Instance.AddJwtBearer();
        return services;
    }

    public static IServiceCollection AddApiKeys(this IServiceCollection services) {
        OpenApiMetadata.Instance.AddApiKeys();
        return services;
    }

    internal static List<JsonNode> ToOpenApiEnums(this IEnumerable<string>? enums) =>
        enums.Safe().Map(x => (JsonNode)JsonValue.Create(x));
}
