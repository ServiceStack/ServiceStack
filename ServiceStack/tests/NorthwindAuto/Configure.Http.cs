using ServiceStack;

[assembly: HostingStartup(typeof(NorthwindAuto.ConfigureHttp))]

namespace NorthwindAuto;

public class ConfigureHttp : IHostingStartup
{
    public void Configure(IWebHostBuilder builder) => builder
        .ConfigureServices(services => services.AddHttpUtilsClient());
}