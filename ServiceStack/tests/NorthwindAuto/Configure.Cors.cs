[assembly: HostingStartup(typeof(NorthwindAuto.ConfigureCors))]

namespace NorthwindAuto;

public class ConfigureCors : IHostingStartup
{
    public void Configure(IWebHostBuilder builder) => builder
        .ConfigureServices(services =>
        {
            services.AddCors(options => {
                options.AddDefaultPolicy(policy => {
                    policy.WithOrigins([
                        "http://localhost:5000", "https://localhost:5001",
                        "https://localhost:5173", "http://localhost:5173",
                    ])
                    .AllowCredentials()
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .SetPreflightMaxAge(TimeSpan.FromHours(1));
                });
            });
            services.AddTransient<IStartupFilter, StartupFilter>();
        });

    public class StartupFilter : IStartupFilter
    {
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) => app =>
        {
            app.UseCors();
            next(app);
        };
    }        
}
