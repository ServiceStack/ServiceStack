using ServiceStack;
using ServiceStack.Data;

[assembly: HostingStartup(typeof(MyApp.ConfigureAutoQuery))]

namespace MyApp;

public class ConfigureAutoQuery : IHostingStartup
{
    public void Configure(IWebHostBuilder builder) => builder
        .ConfigureServices(services => {
            Console.WriteLine("ConfigureAutoQuery.ConfigureServices()");
            // Enable Audit History
            services.AddSingleton<ICrudEvents>(c =>
                new OrmLiteCrudEvents(c.Resolve<IDbConnectionFactory>()));

            // For TodosService
            services.AddPlugin(new AutoQueryDataFeature());
            
            // For Bookings https://docs.servicestack.net/autoquery-crud-bookings
            services.AddPlugin(new AutoQueryFeature {
                 MaxLimit = 1000,
                 //IncludeTotal = true,
            });
        })
        .ConfigureAppHost(appHost => {
            appHost.Resolve<ICrudEvents>().InitSchema();
        });
}