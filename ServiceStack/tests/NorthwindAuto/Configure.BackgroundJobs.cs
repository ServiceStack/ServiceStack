using ServiceStack;
using ServiceStack.Jobs;

[assembly: HostingStartup(typeof(MyApp.ConfigureBackgroundJobs))]

namespace MyApp;

public class ConfigureBackgroundJobs : IHostingStartup
{
    public void Configure(IWebHostBuilder builder) => builder
        .ConfigureServices(services =>
        {
            services.AddPlugin(new CommandsFeature());
            services.AddPlugin(new BackgroundsJobFeature());
            services.AddHostedService<JobsHostedService>();
        }).ConfigureAppHost(afterAppHostInit: appHost => {
            var services = appHost.GetApplicationServices();
            var jobs = services.GetRequiredService<IBackgroundJobs>();
            jobs.RecurringCommand<LogCommand>("Every Minute", Schedule.EveryMinute, new() {
                // RunCommand = true // don't persist job
            });
        });
}

public class JobsHostedService(ILogger<JobsHostedService> log, IBackgroundJobs jobs) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await jobs.StartAsync(stoppingToken);
        
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(3));
        var tick = 0;
        var errors = 0;
        while (!stoppingToken.IsCancellationRequested && await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                tick++;
                await jobs.TickAsync();
            }
            catch (Exception e)
            {
                log.LogError(e, "JOBS {Errors}/{Tick} Error in JobsHostedService: {Message}", 
                    ++errors, tick, e.Message);
            }
        }
    }
}

public class LogCommand(ILogger<LogCommand> logger, IBackgroundJobs jobs) : AsyncCommand
{
    private static long count = 0;
    protected override async Task RunAsync(CancellationToken token)
    {
        Interlocked.Increment(ref count);
        var log = Request.CreateJobLogger(jobs, logger);
        log.LogInformation("Log {Count}: Hello from Recurring Command", count);
        log.UpdateStatus("Starting...", $"[{count}] Lets go...");
        for (var i = 0; i < 20; i++)
        {
            log.LogInformation("Waited {count} times", i+1);
            await Task.Delay(1000, token);
        }
        log.UpdateStatus("Fine", "it is done");
    }
}
