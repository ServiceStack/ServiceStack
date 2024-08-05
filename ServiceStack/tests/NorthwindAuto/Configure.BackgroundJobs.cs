using ServiceStack;
using ServiceStack.Jobs;

[assembly: HostingStartup(typeof(MyApp.ConfigureBackgroundJobs))]

namespace MyApp;

public class ConfigureBackgroundJobs : IHostingStartup
{
    public void Configure(IWebHostBuilder builder) => builder
        .ConfigureServices(services => {
            services.AddPlugin(new CommandsFeature());
            services.AddPlugin(new BackgroundsJobFeature());
            services.AddHostedService<JobsHostedService>();
        })
        .ConfigureAppHost(afterAppHostInit: appHost => {
            appHost.GetPlugin<BackgroundsJobFeature>().Start();
        });
}

public class JobsHostedService(IBackgroundJobs jobs) : IHostedService, IDisposable
{
    private Timer? timer;
    public Task StartAsync(CancellationToken stoppingToken)
    {
        timer = new Timer(DoWork, null, TimeSpan.Zero, TimeSpan.FromSeconds(5));
        return Task.CompletedTask;
    }
    private void DoWork(object? state)
    {
        jobs.Tick();
    }

    public Task StopAsync(CancellationToken stoppingToken)
    {
        timer?.Change(Timeout.Infinite, 0);
        jobs.Dispose();
        return Task.CompletedTask;
    }
    public void Dispose() => timer?.Dispose();
}
