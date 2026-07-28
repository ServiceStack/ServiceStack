using System.Text;
using ServiceStack.Text;

namespace ServiceStack.AI;

/// <summary>
/// Reads the lines of a provider's SSE response, capping the time between chunks rather than the
/// time the whole response takes (the equivalent of aiohttp's sock_read with no total timeout).
///
/// A streamed response can legitimately take much longer to read than a single request/response, and
/// HttpClient.Timeout covers reading the body: it kills long responses mid-stream, which then get
/// retried from scratch and overwrite what was already streamed into the thread. Streaming requests
/// therefore run with an infinite HttpClient timeout and let this enforce the per-chunk deadline.
/// </summary>
public sealed class SseReader(
    Stream stream,
    CancellationToken token,
    TimeSpan readTimeout,
    string providerName) : IAsyncDisposable
{
    readonly StreamReader reader = new(stream, Encoding.UTF8);
    readonly CancellationTokenSource cts = CancellationTokenSource.CreateLinkedTokenSource(token);

    public static async Task<SseReader> CreateAsync(HttpResponseMessage httpRes, ChatContext context,
        TimeSpan readTimeout, string providerName)
    {
        var stream = await httpRes.Content.ReadAsStreamAsync(context.CancellationToken).ConfigAwait();
        return new SseReader(stream, context.CancellationToken, readTimeout, providerName);
    }

    /// <summary>The next line of the stream, or null at the end of it</summary>
    public async Task<string?> ReadLineAsync()
    {
        cts.CancelAfter(readTimeout); // restarts the deadline for each chunk
        try
        {
            return await reader.ReadLineAsync(cts.Token).ConfigAwait();
        }
        catch (OperationCanceledException) when (!token.IsCancellationRequested)
        {
            throw new TimeoutException(
                $"Provider {providerName} stopped streaming after {readTimeout.TotalSeconds:0}s");
        }
    }

    public async ValueTask DisposeAsync()
    {
        reader.Dispose();
        await stream.DisposeAsync().ConfigAwait();
        cts.Dispose();
    }
}
