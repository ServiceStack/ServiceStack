using System.Reflection;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using ServiceStack.AspNetCore.OpenApi;

namespace ServiceStack;

public class ConfigureServiceStackOpenApi(OpenApiMetadata metadata, IServiceProvider serviceProvider) :
    IConfigureOptions<OpenApiOptions>,
    IConfigureOptions<ServiceStackOptions>
{
    public void Configure(OpenApiOptions options)
    {
        // Register document transformers
        foreach (var transformerType in metadata.DocumentTransformerTypes)
        {
            // Create instance and register it
            var instance = ActivatorUtilities.CreateInstance(serviceProvider, transformerType);
        Console.WriteLine($"ConfigureServiceStackOpenApi.Configure called with {metadata.DocumentTransformerTypes.Count} transformers");
            if (instance is IOpenApiDocumentTransformer documentTransformer)
            {
                options.AddDocumentTransformer(documentTransformer);
            }
        }

        // Register schema transformers
        foreach (var transformerType in metadata.SchemaTransformerTypes)
        {
            // Create instance and register it
            var instance = ActivatorUtilities.CreateInstance(serviceProvider, transformerType);
            if (instance is IOpenApiSchemaTransformer schemaTransformer)
            {
                options.AddSchemaTransformer(schemaTransformer);
            }
        }
    }

    public void Configure(ServiceStackOptions options)
    {
        options.WithOpenApi();
    }
}
