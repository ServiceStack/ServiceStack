using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;
using MyApp.Data;
using Scalar.AspNetCore;

[assembly: HostingStartup(typeof(MyApp.ConfigureOpenApi))]

namespace MyApp;

public class ConfigureOpenApi : IHostingStartup
{
    public void Configure(IWebHostBuilder builder) => builder
        .ConfigureServices((context, services) => {
            if (context.HostingEnvironment.IsDevelopment())
            {
                services.AddOpenApi(options =>
                {
                    options.AddOperationTransformer<OpenApiDisplayNameOperationTransformer>();
                });

                services.AddServiceStackOpenApi(configure: metadata =>
                {
                    // metadata.AddBasicAuth();
                    // metadata.AddJwtBearer();
                });
            }
        })
        .Configure((context, app) => {
            if (context.HostingEnvironment.IsDevelopment())
            {
                app.UseRouting();
                app.UseEndpoints(endpoints =>
                {
                    endpoints.MapOpenApi();
                    endpoints.MapScalarApiReference();
                });
            }
        });
}

public class OpenApiDisplayNameOperationTransformer : IOpenApiOperationTransformer
{
    public Task TransformAsync(OpenApiOperation operation, OpenApiOperationTransformerContext context, CancellationToken cancellationToken)
    {
        if (context.Description.ActionDescriptor.EndpointMetadata.FirstOrDefault(x =>
                x is OpenApiDisplayNameAttribute) is OpenApiDisplayNameAttribute attr)
        {
            operation.Extensions ??= new Dictionary<string, IOpenApiExtension>();
            operation.Extensions["x-displayName"] = new OpenApiStringExtension(attr.DisplayName);
        }
        return Task.CompletedTask;
    }
}

// Custom IOpenApiExtension implementation for string values
public class OpenApiStringExtension : IOpenApiExtension
{
    private readonly string _value;

    public OpenApiStringExtension(string value)
    {
        _value = value;
    }

    public void Write(IOpenApiWriter writer, OpenApiSpecVersion specVersion)
    {
        writer.WriteValue(_value);
    }
}

[AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
public class OpenApiDisplayNameAttribute : Attribute
{
    public string DisplayName { get; set;}
}
