using System;
using System.Collections.Generic;
using System.Linq;
using ServiceStack.Text;

namespace ServiceStack.Redis.Generic
{
    /// <summary>
    /// Queue of commands for redis typed client
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class RedisTypedCommandQueue<T> : RedisQueueCompletableOperation
    {
        internal readonly RedisTypedClient<T> RedisClient;
        internal RedisTypedCommandQueue(RedisTypedClient<T> redisClient)
        {
            RedisClient = redisClient;

        }

        public void QueueCommand(Action<IRedisTypedClient<T>> command)
        {
            QueueCommand(command, null, null);
        }

        public void QueueCommand(Action<IRedisTypedClient<T>> command, Action onSuccessCallback)
        {
            QueueCommand(command, onSuccessCallback, null);
        }

        public void QueueCommand(Action<IRedisTypedClient<T>> command, Action onSuccessCallback, Action<Exception> onErrorCallback)
        {
            BeginQueuedCommand(new QueuedRedisTypedCommand<T>
                {
                    VoidReturnCommand = command,
                    OnSuccessVoidCallback = onSuccessCallback,
                    OnErrorCallback = onErrorCallback
                });
            command(RedisClient);
        }


        public void QueueCommand(Func<IRedisTypedClient<T>, int> command)
        {
            QueueCommand(command, null, null);
        }

        public void QueueCommand(Func<IRedisTypedClient<T>, int> command, Action<int> onSuccessCallback)
        {
            QueueCommand(command, onSuccessCallback, null);
        }

        public void QueueCommand(Func<IRedisTypedClient<T>, int> command, Action<int> onSuccessCallback, Action<Exception> onErrorCallback)
        {
            BeginQueuedCommand(new QueuedRedisTypedCommand<T>
            {
                IntReturnCommand = command,
                OnSuccessIntCallback = onSuccessCallback,
                OnErrorCallback = onErrorCallback
            });
            command(RedisClient);
        }


        public void QueueCommand(Func<IRedisTypedClient<T>, long> command)
        {
            QueueCommand(command, null, null);
        }

        public void QueueCommand(Func<IRedisTypedClient<T>, long> command, Action<long> onSuccessCallback)
        {
            QueueCommand(command, onSuccessCallback, null);
        }

        public void QueueCommand(Func<IRedisTypedClient<T>, long> command, Action<long> onSuccessCallback, Action<Exception> onErrorCallback)
        {
            BeginQueuedCommand(new QueuedRedisTypedCommand<T>
            {
                LongReturnCommand = command,
                OnSuccessLongCallback = onSuccessCallback,
                OnErrorCallback = onErrorCallback
            });
            command(RedisClient);
        }


        public void QueueCommand(Func<IRedisTypedClient<T>, bool> command)
        {
            QueueCommand(command, null, null);
        }

        public void QueueCommand(Func<IRedisTypedClient<T>, bool> command, Action<bool> onSuccessCallback)
        {
            QueueCommand(command, onSuccessCallback, null);
        }

        public void QueueCommand(Func<IRedisTypedClient<T>, bool> command, Action<bool> onSuccessCallback, Action<Exception> onErrorCallback)
        {
            BeginQueuedCommand(new QueuedRedisTypedCommand<T>
                {
                    BoolReturnCommand = command,
                    OnSuccessBoolCallback = onSuccessCallback,
                    OnErrorCallback = onErrorCallback
                });
            command(RedisClient);
        }


        public void QueueCommand(Func<IRedisTypedClient<T>, double> command)
        {
            QueueCommand(command, null, null);
        }

        public void QueueCommand(Func<IRedisTypedClient<T>, double> command, Action<double> onSuccessCallback)
        {
            QueueCommand(command, onSuccessCallback, null);
        }

        public void QueueCommand(Func<IRedisTypedClient<T>, double> command, Action<double> onSuccessCallback, Action<Exception> onErrorCallback)
        {
            BeginQueuedCommand(new QueuedRedisTypedCommand<T>
                {
                    DoubleReturnCommand = command,
                    OnSuccessDoubleCallback = onSuccessCallback,
                    OnErrorCallback = onErrorCallback
                });
            command(RedisClient);
        }


        public void QueueCommand(Func<IRedisTypedClient<T>, byte[]> command)
        {
            QueueCommand(command, null, null);
        }

        public void QueueCommand(Func<IRedisTypedClient<T>, byte[]> command, Action<byte[]> onSuccessCallback)
        {
            QueueCommand(command, onSuccessCallback, null);
        }

        public void QueueCommand(Func<IRedisTypedClient<T>, byte[]> command, Action<byte[]> onSuccessCallback, Action<Exception> onErrorCallback)
        {
            BeginQueuedCommand(new QueuedRedisTypedCommand<T>
                {
                    BytesReturnCommand = command,
                    OnSuccessBytesCallback = onSuccessCallback,
                    OnErrorCallback = onErrorCallback
                });
            command(RedisClient);
        }


        public void QueueCommand(Func<IRedisTypedClient<T>, string> command)
        {
            QueueCommand(command, null, null);
        }

        public void QueueCommand(Func<IRedisTypedClient<T>, string> command, Action<string> onSuccessCallback)
        {
            QueueCommand(command, onSuccessCallback, null);
        }

        public void QueueCommand(Func<IRedisTypedClient<T>, string> command, Action<string> onSuccessCallback, Action<Exception> onErrorCallback)
        {
            BeginQueuedCommand(new QueuedRedisTypedCommand<T>
                {
                    StringReturnCommand = command,
                    OnSuccessStringCallback = onSuccessCallback,
                    OnErrorCallback = onErrorCallback
                });
            command(RedisClient);
        }

        public void QueueCommand(Func<IRedisTypedClient<T>, T> command)
        {
            QueueCommand(command, null, null);
        }

        public void QueueCommand(Func<IRedisTypedClient<T>, T> command, Action<T> onSuccessCallback)
        {
            QueueCommand(command, onSuccessCallback, null);
        }

        public void QueueCommand(Func<IRedisTypedClient<T>, T> command, Action<T> onSuccessCallback, Action<Exception> onErrorCallback)
        {
            BeginQueuedCommand(new QueuedRedisTypedCommand<T>
            {
                ObjectReturnCommand = command,
                OnSuccessTypeCallback = x => onSuccessCallback(JsonSerializer.DeserializeFromString<T>(x)),
                OnErrorCallback = onErrorCallback
            });
            command(RedisClient);
        }


        public void QueueCommand(Func<IRedisTypedClient<T>, byte[][]> command)
        {
            QueueCommand(command, null, null);
        }

        public void QueueCommand(Func<IRedisTypedClient<T>, byte[][]> command, Action<byte[][]> onSuccessCallback)
        {
            QueueCommand(command, onSuccessCallback, null);
        }

        public void QueueCommand(Func<IRedisTypedClient<T>, byte[][]> command, Action<byte[][]> onSuccessCallback, Action<Exception> onErrorCallback)
        {
            BeginQueuedCommand(new QueuedRedisTypedCommand<T>
                {
                    MultiBytesReturnCommand = command,
                    OnSuccessMultiBytesCallback = onSuccessCallback,
                    OnErrorCallback = onErrorCallback
                });
            command(RedisClient);
        }


        public void QueueCommand(Func<IRedisTypedClient<T>, List<string>> command)
        {
            QueueCommand(command, null, null);
        }

        public void QueueCommand(Func<IRedisTypedClient<T>, List<string>> command, Action<List<string>> onSuccessCallback)
        {
            QueueCommand(command, onSuccessCallback, null);
        }

        public void QueueCommand(Func<IRedisTypedClient<T>, List<string>> command, Action<List<string>> onSuccessCallback, Action<Exception> onErrorCallback)
        {
            BeginQueuedCommand(new QueuedRedisTypedCommand<T>
                {
                    MultiStringReturnCommand = command,
                    OnSuccessMultiStringCallback = onSuccessCallback,
                    OnErrorCallback = onErrorCallback
                });
            command(RedisClient);
        }

        public void QueueCommand(Func<IRedisTypedClient<T>, List<T>> command)
        {
            QueueCommand(command, null, null);
        }

        public void QueueCommand(Func<IRedisTypedClient<T>, List<T>> command, Action<List<T>> onSuccessCallback)
        {
            QueueCommand(command, onSuccessCallback, null);
        }

        public void QueueCommand(Func<IRedisTypedClient<T>, List<T>> command, Action<List<T>> onSuccessCallback, Action<Exception> onErrorCallback)
        {
            BeginQueuedCommand(new QueuedRedisTypedCommand<T>
            {
                MultiObjectReturnCommand = command,
                OnSuccessMultiTypeCallback = x => onSuccessCallback(x.ConvertAll(y => JsonSerializer.DeserializeFromString<T>(y))),
                OnErrorCallback = onErrorCallback
            });
            command(RedisClient);
        }


        public void QueueCommand(Func<IRedisTypedClient<T>, HashSet<string>> command)
        {
            QueueCommand(command, null, null);
        }

        public void QueueCommand(Func<IRedisTypedClient<T>, HashSet<string>> command, Action<HashSet<string>> onSuccessCallback)
        {
            QueueCommand(command, onSuccessCallback, null);
        }

        public void QueueCommand(Func<IRedisTypedClient<T>, HashSet<string>> command, Action<HashSet<string>> onSuccessCallback, Action<Exception> onErrorCallback)
        {
            BeginQueuedCommand(new QueuedRedisTypedCommand<T>
            {
                MultiStringReturnCommand = r => command(r).ToList(),
                OnSuccessMultiStringCallback = list => onSuccessCallback(list.ToSet()),
                OnErrorCallback = onErrorCallback
            });
            command(RedisClient);
        }

        public void QueueCommand(Func<IRedisTypedClient<T>, HashSet<T>> command)
        {
            QueueCommand(command, null, null);
        }

        public void QueueCommand(Func<IRedisTypedClient<T>, HashSet<T>> command, Action<HashSet<T>> onSuccessCallback)
        {
            QueueCommand(command, onSuccessCallback, null);
        }

        public void QueueCommand(Func<IRedisTypedClient<T>, HashSet<T>> command, Action<HashSet<T>> onSuccessCallback, Action<Exception> onErrorCallback)
        {
            BeginQueuedCommand(new QueuedRedisTypedCommand<T>
            {
                MultiObjectReturnCommand = r => command(r).ToList(),
                OnSuccessMultiTypeCallback = x => onSuccessCallback(x.ConvertAll(JsonSerializer.DeserializeFromString<T>).ToSet()),
                OnErrorCallback = onErrorCallback
            });
            command(RedisClient);
        }

    }
}
