[assembly: HostingStartup(typeof(MyApp.ConfigureCommands))]

namespace MyApp;

public class ConfigureCommands : IHostingStartup
{
    public void Configure(IWebHostBuilder builder) => builder
        .ConfigureServices(services =>
        {
            services.AddPlugin(new CommandsFeature {
            });
        });
}
