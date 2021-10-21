using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ServiceStack.Redis.Generic
{
    /// <summary>
    /// interface to queueable operation using typed redis client
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IRedisTypedQueueableOperationAsync<T>
    {
        void QueueCommand(Func<IRedisTypedClientAsync<T>, ValueTask> command, Action onSuccessCallback = null, Action<Exception> onErrorCallback = null);
        void QueueCommand(Func<IRedisTypedClientAsync<T>, ValueTask<int>> command, Action<int> onSuccessCallback = null, Action<Exception> onErrorCallback = null);
        void QueueCommand(Func<IRedisTypedClientAsync<T>, ValueTask<long>> command, Action<long> onSuccessCallback = null, Action<Exception> onErrorCallback = null);
        void QueueCommand(Func<IRedisTypedClientAsync<T>, ValueTask<bool>> command, Action<bool> onSuccessCallback = null, Action<Exception> onErrorCallback = null);
        void QueueCommand(Func<IRedisTypedClientAsync<T>, ValueTask<double>> command, Action<double> onSuccessCallback = null, Action<Exception> onErrorCallback = null);
        void QueueCommand(Func<IRedisTypedClientAsync<T>, ValueTask<byte[]>> command, Action<byte[]> onSuccessCallback = null, Action<Exception> onErrorCallback = null);
        void QueueCommand(Func<IRedisTypedClientAsync<T>, ValueTask<string>> command, Action<string> onSuccessCallback = null, Action<Exception> onErrorCallback = null);
        void QueueCommand(Func<IRedisTypedClientAsync<T>, ValueTask<T>> command, Action<T> onSuccessCallback = null, Action<Exception> onErrorCallback = null);
        void QueueCommand(Func<IRedisTypedClientAsync<T>, ValueTask<List<string>>> command, Action<List<string>> onSuccessCallback = null, Action<Exception> onErrorCallback = null);
        void QueueCommand(Func<IRedisTypedClientAsync<T>, ValueTask<HashSet<string>>> command, Action<HashSet<string>> onSuccessCallback = null, Action<Exception> onErrorCallback = null);
        void QueueCommand(Func<IRedisTypedClientAsync<T>, ValueTask<List<T>>> command, Action<List<T>> onSuccessCallback = null, Action<Exception> onErrorCallback = null);

    }
}