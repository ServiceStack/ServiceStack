using ServiceStack;

[assembly: HostingStartup(typeof(CheckWebCore.ConfigureServerEvents))]

namespace CheckWebCore;

public class ConfigureServerEvents : IHostingStartup
{
    public void Configure(IWebHostBuilder builder) => builder
        .ConfigureServices(services => {
            services.AddPlugin(new ServerEventsFeature());
        });
}
