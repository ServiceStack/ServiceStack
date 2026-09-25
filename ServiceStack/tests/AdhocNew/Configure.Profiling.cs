using ServiceStack;

[assembly: HostingStartup(typeof(AdhocNew.ConfigureProfiling))]

namespace AdhocNew;

public class ConfigureProfiling : IHostingStartup
{
    public void Configure(IWebHostBuilder builder) => builder
        .ConfigureServices((context, services) => {
            if (context.HostingEnvironment.IsDevelopment())
            {
                services.AddPlugin(new ProfilingFeature
                {
                    IncludeStackTrace = true,
                });
            }
        });
}
