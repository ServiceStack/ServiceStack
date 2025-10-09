using Microsoft.Extensions.Diagnostics.HealthChecks;

[assembly: HostingStartup(typeof(MyApp.HealthChecks))]

namespace MyApp;

public class HealthChecks : IHostingStartup
{
    public class HealthCheck : IHealthCheck
    {
        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken token = default)
        {
            // Perform health check logic here
            return HealthCheckResult.Healthy();
        }
    }

    public void Configure(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            services.AddHealthChecks()
                .AddCheck<HealthCheck>("HealthCheck");

            services.AddTransient<IStartupFilter, StartupFilter>();
        });
    }
    
    public class StartupFilter : IStartupFilter
    {
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
            => app => {
                app.UseHealthChecks("/up");
                next(app);
            };
    }
}