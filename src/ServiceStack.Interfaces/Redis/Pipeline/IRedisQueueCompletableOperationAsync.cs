using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ServiceStack.Redis.Pipeline
{
    /// <summary>
    /// Interface to operations that allow queued commands to be completed
    /// </summary>
    public interface IRedisQueueCompletableOperationAsync
    {
        void CompleteVoidQueuedCommandAsync(Func<CancellationToken, ValueTask> voidReadCommand);
        void CompleteIntQueuedCommandAsync(Func<CancellationToken, ValueTask<int>> intReadCommand);
        void CompleteLongQueuedCommandAsync(Func<CancellationToken, ValueTask<long>> longReadCommand);
        void CompleteBytesQueuedCommandAsync(Func<CancellationToken, ValueTask<byte[]>> bytesReadCommand);
        void CompleteMultiBytesQueuedCommandAsync(Func<CancellationToken, ValueTask<byte[][]>> multiBytesReadCommand);
        void CompleteStringQueuedCommandAsync(Func<CancellationToken, ValueTask<string>> stringReadCommand);
        void CompleteMultiStringQueuedCommandAsync(Func<CancellationToken, ValueTask<List<string>>> multiStringReadCommand);
        void CompleteDoubleQueuedCommandAsync(Func<CancellationToken, ValueTask<double>> doubleReadCommand);
        void CompleteRedisDataQueuedCommandAsync(Func<CancellationToken, ValueTask<RedisData>> redisDataReadCommand);
    }
}