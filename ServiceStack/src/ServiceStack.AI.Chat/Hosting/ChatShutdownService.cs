using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ServiceStack.AI;

internal sealed class ChatShutdownService(ChatFeature feature) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    public Task StopAsync(CancellationToken cancellationToken) => feature.RunAsyncShutdownHandlers(cancellationToken);
}

public partial class ChatFeature
{
    readonly object asyncShutdownLock = new();
    Task? asyncShutdownTask;
    public Task RunAsyncShutdownHandlers(CancellationToken cancellationToken = default)
    {
        lock (asyncShutdownLock) return asyncShutdownTask ??= StopExtensionsAsync(cancellationToken);
    }
    async Task StopExtensionsAsync(CancellationToken cancellationToken)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(15));
        await Task.WhenAll(Filters.AsyncShutdownHandlers.Select(async handler => {
            try { await handler(timeout.Token).WaitAsync(timeout.Token).ConfigureAwait(false); }
            catch (Exception e) { Log.LogError(e, "Asynchronous extension shutdown failed"); }
        })).ConfigureAwait(false);
    }
}
