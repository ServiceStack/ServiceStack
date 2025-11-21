using Microsoft.Extensions.Options;
using ServiceStack.AspNetCore.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace ServiceStack;

public class ConfigureServiceStackSwagger(OpenApiMetadata metadata) : 
    IConfigureOptions<SwaggerGenOptions>, 
    IConfigureOptions<ServiceStackOptions>
{
    public void Configure(SwaggerGenOptions options)
    {
        foreach (var filterType in metadata.DocumentFilterTypes)
        {
            options.DocumentFilterDescriptors.Add(new FilterDescriptor
            {
                Type = filterType,
                Arguments = [],
            });
        }
        foreach (var filterType in metadata.SchemaFilterTypes)
        {
            options.SchemaFilterDescriptors.Add(new FilterDescriptor
            {
                Type = filterType,
                Arguments = [],
            });
        }
    }

    public void Configure(ServiceStackOptions options)
    {
        options.WithOpenApi();
    }
}
