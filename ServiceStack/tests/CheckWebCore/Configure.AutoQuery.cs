using Microsoft.AspNetCore.Hosting;
using MyApp.ServiceModel;
using ServiceStack;
using ServiceStack.Data;

// In Configure.AppHost
//[assembly: HostingStartup(typeof(MyApp.ConfigureAutoQuery))]

namespace MyApp;

public class ConfigureAutoQuery : IConfigureAppHost, IConfigureServices // : IHostingStartup
{
    public void Configure(IWebHostBuilder builder) => builder
        .ConfigureServices(Configure)
        .ConfigureAppHost(Configure);

    public void Configure(IServiceCollection services)
    {
        // Enable Audit History
        services.AddSingleton<ICrudEvents>(c =>
            new OrmLiteCrudEvents(c.Resolve<IDbConnectionFactory>()));
    }

    public void Configure(IAppHost appHost)
    {
        // For TodosService
        appHost.Plugins.Add(new AutoQueryDataFeature());

        // For NorthwindAuto + Bookings
        appHost.Plugins.Add(new AutoQueryFeature {
            MaxLimit = 100,
            GenerateCrudServices = new GenerateCrudServices {
                AutoRegister = true,
                ServiceFilter = (op, req) =>
                {
                    op.Request.AddAttributeIfNotExists(new TagAttribute("Northwind"));
                    if (op.Request.Name.IndexOf("User", StringComparison.Ordinal) >= 0)
                        op.Request.AddAttributeIfNotExists(new ValidateIsAdminAttribute());
                },
                IncludeService = op => !op.ReferencesAny(nameof(Booking)),
            },
        });

        appHost.Resolve<ICrudEvents>().InitSchema();
    }
}
