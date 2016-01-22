using System;
using System.Collections.Generic;

namespace ServiceStack.Redis.Generic
{
    /// <summary>
    /// interface to queueable operation using typed redis client
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IRedisTypedQueueableOperation<T>
    {
        void QueueCommand(Action<IRedisTypedClient<T>> command);
        void QueueCommand(Action<IRedisTypedClient<T>> command, Action onSuccessCallback);
        void QueueCommand(Action<IRedisTypedClient<T>> command, Action onSuccessCallback, Action<Exception> onErrorCallback);

        void QueueCommand(Func<IRedisTypedClient<T>, int> command);
        void QueueCommand(Func<IRedisTypedClient<T>, int> command, Action<int> onSuccessCallback);
        void QueueCommand(Func<IRedisTypedClient<T>, int> command, Action<int> onSuccessCallback, Action<Exception> onErrorCallback);

        void QueueCommand(Func<IRedisTypedClient<T>, long> command);
        void QueueCommand(Func<IRedisTypedClient<T>, long> command, Action<long> onSuccessCallback);
        void QueueCommand(Func<IRedisTypedClient<T>, long> command, Action<long> onSuccessCallback, Action<Exception> onErrorCallback);

        void QueueCommand(Func<IRedisTypedClient<T>, bool> command);
        void QueueCommand(Func<IRedisTypedClient<T>, bool> command, Action<bool> onSuccessCallback);
        void QueueCommand(Func<IRedisTypedClient<T>, bool> command, Action<bool> onSuccessCallback, Action<Exception> onErrorCallback);

        void QueueCommand(Func<IRedisTypedClient<T>, double> command);
        void QueueCommand(Func<IRedisTypedClient<T>, double> command, Action<double> onSuccessCallback);
        void QueueCommand(Func<IRedisTypedClient<T>, double> command, Action<double> onSuccessCallback, Action<Exception> onErrorCallback);

        void QueueCommand(Func<IRedisTypedClient<T>, byte[]> command);
        void QueueCommand(Func<IRedisTypedClient<T>, byte[]> command, Action<byte[]> onSuccessCallback);
        void QueueCommand(Func<IRedisTypedClient<T>, byte[]> command, Action<byte[]> onSuccessCallback, Action<Exception> onErrorCallback);

        void QueueCommand(Func<IRedisTypedClient<T>, string> command);
        void QueueCommand(Func<IRedisTypedClient<T>, string> command, Action<string> onSuccessCallback);
        void QueueCommand(Func<IRedisTypedClient<T>, string> command, Action<string> onSuccessCallback, Action<Exception> onErrorCallback);

        void QueueCommand(Func<IRedisTypedClient<T>, T> command);
        void QueueCommand(Func<IRedisTypedClient<T>, T> command, Action<T> onSuccessCallback);
        void QueueCommand(Func<IRedisTypedClient<T>, T> command, Action<T> onSuccessCallback, Action<Exception> onErrorCallback);

        void QueueCommand(Func<IRedisTypedClient<T>, List<string>> command);
        void QueueCommand(Func<IRedisTypedClient<T>, List<string>> command, Action<List<string>> onSuccessCallback);
        void QueueCommand(Func<IRedisTypedClient<T>, List<string>> command, Action<List<string>> onSuccessCallback, Action<Exception> onErrorCallback);

        void QueueCommand(Func<IRedisTypedClient<T>, HashSet<string>> command);
        void QueueCommand(Func<IRedisTypedClient<T>, HashSet<string>> command, Action<HashSet<string>> onSuccessCallback);
        void QueueCommand(Func<IRedisTypedClient<T>, HashSet<string>> command, Action<HashSet<string>> onSuccessCallback, Action<Exception> onErrorCallback);

        void QueueCommand(Func<IRedisTypedClient<T>, List<T>> command);
        void QueueCommand(Func<IRedisTypedClient<T>, List<T>> command, Action<List<T>> onSuccessCallback);
        void QueueCommand(Func<IRedisTypedClient<T>, List<T>> command, Action<List<T>> onSuccessCallback, Action<Exception> onErrorCallback);

    }
}
