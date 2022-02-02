//
// https://github.com/ServiceStack/ServiceStack.Redis
// ServiceStack.Redis: ECMA CLI Binding to the Redis key-value storage system
//
// Authors:
//   Demis Bellot (demis.bellot@gmail.com)
//
// Copyright 2013 ServiceStack, Inc. All Rights Reserved.
//
// Licensed under the same terms of ServiceStack.
//

using System;
using System.Collections.Generic;
using System.Linq;
using ServiceStack.Redis.Pipeline;

namespace ServiceStack.Redis
{
    /// <summary>
    /// </summary>
    public class RedisCommandQueue : RedisQueueCompletableOperation
    {
        protected readonly RedisClient RedisClient;

        public RedisCommandQueue(RedisClient redisClient)
        {
            this.RedisClient = redisClient;

        }

        public void QueueCommand(Action<IRedisClient> command)
        {
            QueueCommand(command, null, null);
        }

        public void QueueCommand(Action<IRedisClient> command, Action onSuccessCallback)
        {
            QueueCommand(command, onSuccessCallback, null);
        }

        public virtual void QueueCommand(Action<IRedisClient> command, Action onSuccessCallback, Action<Exception> onErrorCallback)
        {
            BeginQueuedCommand(new QueuedRedisCommand
            {
                VoidReturnCommand = command,
                OnSuccessVoidCallback = onSuccessCallback,
                OnErrorCallback = onErrorCallback
            });
            command(RedisClient);
        }


        public void QueueCommand(Func<IRedisClient, int> command)
        {
            QueueCommand(command, null, null);
        }

        public void QueueCommand(Func<IRedisClient, int> command, Action<int> onSuccessCallback)
        {
            QueueCommand(command, onSuccessCallback, null);
        }

        public virtual void QueueCommand(Func<IRedisClient, int> command, Action<int> onSuccessCallback, Action<Exception> onErrorCallback)
        {
            BeginQueuedCommand(new QueuedRedisCommand
            {
                IntReturnCommand = command,
                OnSuccessIntCallback = onSuccessCallback,
                OnErrorCallback = onErrorCallback
            });
            command(RedisClient);
        }


        public void QueueCommand(Func<IRedisClient, long> command)
        {
            QueueCommand(command, null, null);
        }

        public void QueueCommand(Func<IRedisClient, long> command, Action<long> onSuccessCallback)
        {
            QueueCommand(command, onSuccessCallback, null);
        }

        public virtual void QueueCommand(Func<IRedisClient, long> command, Action<long> onSuccessCallback, Action<Exception> onErrorCallback)
        {
            BeginQueuedCommand(new QueuedRedisCommand
            {
                LongReturnCommand = command,
                OnSuccessLongCallback = onSuccessCallback,
                OnErrorCallback = onErrorCallback
            });
            command(RedisClient);
        }


        public void QueueCommand(Func<IRedisClient, bool> command)
        {
            QueueCommand(command, null, null);
        }

        public void QueueCommand(Func<IRedisClient, bool> command, Action<bool> onSuccessCallback)
        {
            QueueCommand(command, onSuccessCallback, null);
        }

        public virtual void QueueCommand(Func<IRedisClient, bool> command, Action<bool> onSuccessCallback, Action<Exception> onErrorCallback)
        {
            BeginQueuedCommand(new QueuedRedisCommand
            {
                BoolReturnCommand = command,
                OnSuccessBoolCallback = onSuccessCallback,
                OnErrorCallback = onErrorCallback
            });
            command(RedisClient);
        }


        public void QueueCommand(Func<IRedisClient, double> command)
        {
            QueueCommand(command, null, null);
        }

        public void QueueCommand(Func<IRedisClient, double> command, Action<double> onSuccessCallback)
        {
            QueueCommand(command, onSuccessCallback, null);
        }

        public virtual void QueueCommand(Func<IRedisClient, double> command, Action<double> onSuccessCallback, Action<Exception> onErrorCallback)
        {
            BeginQueuedCommand(new QueuedRedisCommand
            {
                DoubleReturnCommand = command,
                OnSuccessDoubleCallback = onSuccessCallback,
                OnErrorCallback = onErrorCallback
            });
            command(RedisClient);
        }


        public void QueueCommand(Func<IRedisClient, byte[]> command)
        {
            QueueCommand(command, null, null);
        }

        public void QueueCommand(Func<IRedisClient, byte[]> command, Action<byte[]> onSuccessCallback)
        {
            QueueCommand(command, onSuccessCallback, null);
        }

        public virtual void QueueCommand(Func<IRedisClient, byte[]> command, Action<byte[]> onSuccessCallback, Action<Exception> onErrorCallback)
        {
            BeginQueuedCommand(new QueuedRedisCommand
            {
                BytesReturnCommand = command,
                OnSuccessBytesCallback = onSuccessCallback,
                OnErrorCallback = onErrorCallback
            });
            command(RedisClient);
        }


        public void QueueCommand(Func<IRedisClient, string> command)
        {
            QueueCommand(command, null, null);
        }

        public void QueueCommand(Func<IRedisClient, string> command, Action<string> onSuccessCallback)
        {
            QueueCommand(command, onSuccessCallback, null);
        }

        public virtual void QueueCommand(Func<IRedisClient, string> command, Action<string> onSuccessCallback, Action<Exception> onErrorCallback)
        {
            BeginQueuedCommand(new QueuedRedisCommand
            {
                StringReturnCommand = command,
                OnSuccessStringCallback = onSuccessCallback,
                OnErrorCallback = onErrorCallback
            });
            command(RedisClient);
        }


        public void QueueCommand(Func<IRedisClient, byte[][]> command)
        {
            QueueCommand(command, null, null);
        }

        public void QueueCommand(Func<IRedisClient, byte[][]> command, Action<byte[][]> onSuccessCallback)
        {
            QueueCommand(command, onSuccessCallback, null);
        }

        public virtual void QueueCommand(Func<IRedisClient, byte[][]> command, Action<byte[][]> onSuccessCallback, Action<Exception> onErrorCallback)
        {
            BeginQueuedCommand(new QueuedRedisCommand
            {
                MultiBytesReturnCommand = command,
                OnSuccessMultiBytesCallback = onSuccessCallback,
                OnErrorCallback = onErrorCallback
            });
            command(RedisClient);
        }


        public void QueueCommand(Func<IRedisClient, List<string>> command)
        {
            QueueCommand(command, null, null);
        }

        public void QueueCommand(Func<IRedisClient, List<string>> command, Action<List<string>> onSuccessCallback)
        {
            QueueCommand(command, onSuccessCallback, null);
        }

        public virtual void QueueCommand(Func<IRedisClient, List<string>> command, Action<List<string>> onSuccessCallback, Action<Exception> onErrorCallback)
        {
            BeginQueuedCommand(new QueuedRedisCommand
            {
                MultiStringReturnCommand = command,
                OnSuccessMultiStringCallback = onSuccessCallback,
                OnErrorCallback = onErrorCallback
            });
            command(RedisClient);
        }


        public void QueueCommand(Func<IRedisClient, HashSet<string>> command)
        {
            QueueCommand(command, null, null);
        }

        public void QueueCommand(Func<IRedisClient, HashSet<string>> command, Action<HashSet<string>> onSuccessCallback)
        {
            QueueCommand(command, onSuccessCallback, null);
        }

        public virtual void QueueCommand(Func<IRedisClient, HashSet<string>> command, Action<HashSet<string>> onSuccessCallback, Action<Exception> onErrorCallback)
        {
            BeginQueuedCommand(new QueuedRedisCommand
            {
                MultiStringReturnCommand = r => command(r).ToList(),
                OnSuccessMultiStringCallback = list => onSuccessCallback(list.ToSet()),
                OnErrorCallback = onErrorCallback
            });
            command(RedisClient);
        }


        public void QueueCommand(Func<IRedisClient, Dictionary<string, string>> command)
        {
            QueueCommand(command, null, null);
        }

        public void QueueCommand(Func<IRedisClient, Dictionary<string, string>> command, Action<Dictionary<string, string>> onSuccessCallback)
        {
            QueueCommand(command, onSuccessCallback, null);
        }

        public void QueueCommand(Func<IRedisClient, Dictionary<string, string>> command, Action<Dictionary<string, string>> onSuccessCallback, Action<Exception> onErrorCallback)
        {
            BeginQueuedCommand(new QueuedRedisCommand
            {
                DictionaryStringReturnCommand = command,
                OnSuccessDictionaryStringCallback = onSuccessCallback,
                OnErrorCallback = onErrorCallback
            });
            command(RedisClient);
        }


        public void QueueCommand(Func<IRedisClient, RedisData> command)
        {
            QueueCommand(command, null, null);
        }

        public void QueueCommand(Func<IRedisClient, RedisData> command, Action<RedisData> onSuccessCallback)
        {
            QueueCommand(command, onSuccessCallback, null);
        }

        public void QueueCommand(Func<IRedisClient, RedisData> command, Action<RedisData> onSuccessCallback, Action<Exception> onErrorCallback)
        {
            BeginQueuedCommand(new QueuedRedisCommand
            {
                RedisDataReturnCommand = command,
                OnSuccessRedisDataCallback = onSuccessCallback,
                OnErrorCallback = onErrorCallback
            });
            command(RedisClient);
        }


        public void QueueCommand(Func<IRedisClient, RedisText> command)
        {
            QueueCommand(command, null, null);
        }

        public void QueueCommand(Func<IRedisClient, RedisText> command, Action<RedisText> onSuccessCallback)
        {
            QueueCommand(command, onSuccessCallback, null);
        }

        public void QueueCommand(Func<IRedisClient, RedisText> command, Action<RedisText> onSuccessCallback, Action<Exception> onErrorCallback)
        {
            BeginQueuedCommand(new QueuedRedisCommand
            {
                RedisTextReturnCommand = command,
                OnSuccessRedisTextCallback = onSuccessCallback,
                OnErrorCallback = onErrorCallback
            });
            command(RedisClient);
        }
    }
}