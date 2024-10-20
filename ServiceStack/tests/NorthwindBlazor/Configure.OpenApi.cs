using System.ComponentModel;
using System.Reflection;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Extensions;
using Microsoft.OpenApi.Models;
using MyApp.Data;
using Swashbuckle.AspNetCore.SwaggerGen;

[assembly: HostingStartup(typeof(MyApp.ConfigureOpenApi))]

namespace MyApp;

public class ConfigureOpenApi : IHostingStartup
{
    public void Configure(IWebHostBuilder builder) => builder
        .ConfigureServices((context, services) => {
            if (context.HostingEnvironment.IsDevelopment())
            {
                services.AddEndpointsApiExplorer();
                services.AddSwaggerGen(options =>
                {
                    options.OperationFilter<OpenApiDisplayNameOperationFilter>();
                });

                services.AddServiceStackSwagger();
                services.AddBasicAuth<ApplicationUser>();
                //services.AddJwtAuth();
            
                services.AddTransient<IStartupFilter,StartupFilter>();
            }
        });

    public class StartupFilter : IStartupFilter
    {
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) => app =>
        {
            app.UseSwagger();
            app.UseSwaggerUI();
            next(app);
        };
    }
    
    public class OpenApiDisplayNameOperationFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            if (context.ApiDescription.ActionDescriptor.EndpointMetadata.FirstOrDefault(x =>
                    x is OpenApiDisplayNameAttribute) is OpenApiDisplayNameAttribute attr)
            {        
                operation.AddExtension("x-displayName", new OpenApiString(attr.DisplayName));
            }
        }
    }
}
[AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
public class OpenApiDisplayNameAttribute : Attribute
{
    public string DisplayName { get; set;}
}