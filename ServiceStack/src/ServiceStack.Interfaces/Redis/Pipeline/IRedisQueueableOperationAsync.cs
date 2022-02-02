using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ServiceStack.Redis.Pipeline
{
    /// <summary>
    /// interface to operation that can queue commands
    /// </summary>
    public interface IRedisQueueableOperationAsync
    {
        void QueueCommand(Func<IRedisClientAsync, ValueTask> command, Action onSuccessCallback = null, Action<Exception> onErrorCallback = null);
        void QueueCommand(Func<IRedisClientAsync, ValueTask<int>> command, Action<int> onSuccessCallback = null, Action<Exception> onErrorCallback = null);
        void QueueCommand(Func<IRedisClientAsync, ValueTask<long>> command, Action<long> onSuccessCallback = null, Action<Exception> onErrorCallback = null);
        void QueueCommand(Func<IRedisClientAsync, ValueTask<bool>> command, Action<bool> onSuccessCallback = null, Action<Exception> onErrorCallback = null);
        void QueueCommand(Func<IRedisClientAsync, ValueTask<double>> command, Action<double> onSuccessCallback = null, Action<Exception> onErrorCallback = null);
        void QueueCommand(Func<IRedisClientAsync, ValueTask<byte[]>> command, Action<byte[]> onSuccessCallback = null, Action<Exception> onErrorCallback = null);
        void QueueCommand(Func<IRedisClientAsync, ValueTask<byte[][]>> command, Action<byte[][]> onSuccessCallback = null, Action<Exception> onErrorCallback = null);
        void QueueCommand(Func<IRedisClientAsync, ValueTask<string>> command, Action<string> onSuccessCallback = null, Action<Exception> onErrorCallback = null);
        void QueueCommand(Func<IRedisClientAsync, ValueTask<List<string>>> command, Action<List<string>> onSuccessCallback = null, Action<Exception> onErrorCallback = null);
        void QueueCommand(Func<IRedisClientAsync, ValueTask<HashSet<string>>> command, Action<HashSet<string>> onSuccessCallback = null, Action<Exception> onErrorCallback = null);
        void QueueCommand(Func<IRedisClientAsync, ValueTask<Dictionary<string, string>>> command, Action<Dictionary<string, string>> onSuccessCallback = null, Action<Exception> onErrorCallback = null);
        void QueueCommand(Func<IRedisClientAsync, ValueTask<RedisData>> command, Action<RedisData> onSuccessCallback = null, Action<Exception> onErrorCallback = null);
        void QueueCommand(Func<IRedisClientAsync, ValueTask<RedisText>> command, Action<RedisText> onSuccessCallback = null, Action<Exception> onErrorCallback = null);
    }
}