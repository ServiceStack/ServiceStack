using System;
using System.Collections.Generic;

namespace ServiceStack.Redis.Pipeline
{
    /// <summary>
    /// interface to operation that can queue commands
    /// </summary>
    public interface IRedisQueueableOperation
    {
        void QueueCommand(Action<IRedisClient> command);
        void QueueCommand(Action<IRedisClient> command, Action onSuccessCallback);
        void QueueCommand(Action<IRedisClient> command, Action onSuccessCallback, Action<Exception> onErrorCallback);

        void QueueCommand(Func<IRedisClient, int> command);
        void QueueCommand(Func<IRedisClient, int> command, Action<int> onSuccessCallback);
        void QueueCommand(Func<IRedisClient, int> command, Action<int> onSuccessCallback, Action<Exception> onErrorCallback);

        void QueueCommand(Func<IRedisClient, long> command);
        void QueueCommand(Func<IRedisClient, long> command, Action<long> onSuccessCallback);
        void QueueCommand(Func<IRedisClient, long> command, Action<long> onSuccessCallback, Action<Exception> onErrorCallback);

        void QueueCommand(Func<IRedisClient, bool> command);
        void QueueCommand(Func<IRedisClient, bool> command, Action<bool> onSuccessCallback);
        void QueueCommand(Func<IRedisClient, bool> command, Action<bool> onSuccessCallback, Action<Exception> onErrorCallback);

        void QueueCommand(Func<IRedisClient, double> command);
        void QueueCommand(Func<IRedisClient, double> command, Action<double> onSuccessCallback);
        void QueueCommand(Func<IRedisClient, double> command, Action<double> onSuccessCallback, Action<Exception> onErrorCallback);

        void QueueCommand(Func<IRedisClient, byte[]> command);
        void QueueCommand(Func<IRedisClient, byte[]> command, Action<byte[]> onSuccessCallback);
        void QueueCommand(Func<IRedisClient, byte[]> command, Action<byte[]> onSuccessCallback, Action<Exception> onErrorCallback);

        void QueueCommand(Func<IRedisClient, byte[][]> command);
        void QueueCommand(Func<IRedisClient, byte[][]> command, Action<byte[][]> onSuccessCallback);
        void QueueCommand(Func<IRedisClient, byte[][]> command, Action<byte[][]> onSuccessCallback, Action<Exception> onErrorCallback);

        void QueueCommand(Func<IRedisClient, string> command);
        void QueueCommand(Func<IRedisClient, string> command, Action<string> onSuccessCallback);
        void QueueCommand(Func<IRedisClient, string> command, Action<string> onSuccessCallback, Action<Exception> onErrorCallback);

        void QueueCommand(Func<IRedisClient, List<string>> command);
        void QueueCommand(Func<IRedisClient, List<string>> command, Action<List<string>> onSuccessCallback);
        void QueueCommand(Func<IRedisClient, List<string>> command, Action<List<string>> onSuccessCallback, Action<Exception> onErrorCallback);

        void QueueCommand(Func<IRedisClient, Dictionary<string,string>> command);
        void QueueCommand(Func<IRedisClient, Dictionary<string,string>> command, Action<Dictionary<string,string>> onSuccessCallback);
        void QueueCommand(Func<IRedisClient, Dictionary<string,string>> command, Action<Dictionary<string,string>> onSuccessCallback, Action<Exception> onErrorCallback);
    }
}