using ServiceStack;
using ServiceStack.Data;

[assembly: HostingStartup(typeof(MyApp.ConfigureAutoQuery))]

namespace MyApp;

public class ConfigureAutoQuery : IHostingStartup
{
    public void Configure(IWebHostBuilder builder) => builder
        .ConfigureServices(services => {
            // Enable Audit History
            services.AddSingleton<ICrudEvents>(c =>
                new OrmLiteCrudEvents(c.Resolve<IDbConnectionFactory>()));
        })
        .ConfigureAppHost(appHost => {

            // For TodosService
            appHost.Plugins.Add(new AutoQueryDataFeature());

            // For Bookings https://github.com/NetCoreApps/BookingsCrud
            appHost.Plugins.Add(new AutoQueryFeature {
                MaxLimit = 1000,
                //IncludeTotal = true,
            });

            appHost.Resolve<ICrudEvents>().InitSchema();
        });
}
