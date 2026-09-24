#if NET8_0_OR_GREATER
#nullable enable

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ServiceStack.Text;

namespace ServiceStack.Jobs;

/// <summary>
/// Drains Background Jobs when the App shuts down. Registered by the Background Jobs plugins so
/// existing Apps get graceful shutdown without having to change their own Jobs hosted service.
/// </summary>
public class JobsShutdownHostedService(ILogger<JobsShutdownHostedService> log, IServiceProvider services)
    : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        try
        {
            // Resolved lazily: the App may be shutting down before Jobs were ever registered
            if (services.GetService(typeof(IBackgroundJobs)) is not IBackgroundJobs jobs)
                return;
            await jobs.StopAsync(cancellationToken).ConfigAwait();
        }
        catch (Exception e)
        {
            log.LogError(e, "JOBS Error draining Background Jobs on shutdown");
        }
    }
}
#endif
