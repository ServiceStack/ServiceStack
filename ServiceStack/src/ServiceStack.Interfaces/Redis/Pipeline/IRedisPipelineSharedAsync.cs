using System;
using System.Threading;
using System.Threading.Tasks;

namespace ServiceStack.Redis.Pipeline
{
    /// <summary>
    /// Pipeline interface shared by typed and non-typed pipelines
    /// </summary>
    public interface IRedisPipelineSharedAsync : IAsyncDisposable, IRedisQueueCompletableOperationAsync
    {
        ValueTask FlushAsync(CancellationToken token = default);
        ValueTask<bool> ReplayAsync(CancellationToken token = default);
    }
}