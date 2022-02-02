using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ServiceStack.Redis.Generic;
using ServiceStack.Redis.Pipeline;
using ServiceStack.Text;

namespace ServiceStack.Redis
{
    /// <summary>
    /// Pipeline for redis typed client
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public partial class RedisTypedPipeline<T>
        : IRedisTypedPipelineAsync<T>
    {
        private IRedisTypedPipelineAsync<T> AsAsync() => this;
        void IRedisQueueCompletableOperationAsync.CompleteBytesQueuedCommandAsync(Func<CancellationToken, ValueTask<byte[]>> bytesReadCommand)
        {
            //AssertCurrentOperation();
            // this can happen when replaying pipeline/transaction
            if (CurrentQueuedOperation == null) return;

            CurrentQueuedOperation.WithAsyncReadCommand(bytesReadCommand);
            AddCurrentQueuedOperation();
        }

        void IRedisQueueCompletableOperationAsync.CompleteDoubleQueuedCommandAsync(Func<CancellationToken, ValueTask<double>> doubleReadCommand)
        {
            //AssertCurrentOperation();
            // this can happen when replaying pipeline/transaction
            if (CurrentQueuedOperation == null) return;

            CurrentQueuedOperation.WithAsyncReadCommand(doubleReadCommand);
            AddCurrentQueuedOperation();
        }

        void IRedisQueueCompletableOperationAsync.CompleteIntQueuedCommandAsync(Func<CancellationToken, ValueTask<int>> intReadCommand)
        {
            //AssertCurrentOperation();
            // this can happen when replaying pipeline/transaction
            if (CurrentQueuedOperation == null) return;

            CurrentQueuedOperation.WithAsyncReadCommand(intReadCommand);
            AddCurrentQueuedOperation();
        }

        void IRedisQueueCompletableOperationAsync.CompleteLongQueuedCommandAsync(Func<CancellationToken, ValueTask<long>> longReadCommand)
        {
            //AssertCurrentOperation();
            // this can happen when replaying pipeline/transaction
            if (CurrentQueuedOperation == null) return;

            CurrentQueuedOperation.WithAsyncReadCommand(longReadCommand);
            AddCurrentQueuedOperation();
        }

        void IRedisQueueCompletableOperationAsync.CompleteMultiBytesQueuedCommandAsync(Func<CancellationToken, ValueTask<byte[][]>> multiBytesReadCommand)
        {
            //AssertCurrentOperation();
            // this can happen when replaying pipeline/transaction
            if (CurrentQueuedOperation == null) return;

            CurrentQueuedOperation.WithAsyncReadCommand(multiBytesReadCommand);
            AddCurrentQueuedOperation();
        }

        void IRedisQueueCompletableOperationAsync.CompleteMultiStringQueuedCommandAsync(Func<CancellationToken, ValueTask<List<string>>> multiStringReadCommand)
        {
            //AssertCurrentOperation();
            // this can happen when replaying pipeline/transaction
            if (CurrentQueuedOperation == null) return;

            CurrentQueuedOperation.WithAsyncReadCommand(multiStringReadCommand);
            AddCurrentQueuedOperation();
        }

        void IRedisQueueCompletableOperationAsync.CompleteRedisDataQueuedCommandAsync(Func<CancellationToken, ValueTask<RedisData>> redisDataReadCommand)
        {
            //AssertCurrentOperation();
            // this can happen when replaying pipeline/transaction
            if (CurrentQueuedOperation == null) return;

            CurrentQueuedOperation.WithAsyncReadCommand(redisDataReadCommand);
            AddCurrentQueuedOperation();
        }

        void IRedisQueueCompletableOperationAsync.CompleteStringQueuedCommandAsync(Func<CancellationToken, ValueTask<string>> stringReadCommand)
        {
            //AssertCurrentOperation();
            // this can happen when replaying pipeline/transaction
            if (CurrentQueuedOperation == null) return;

            CurrentQueuedOperation.WithAsyncReadCommand(stringReadCommand);
            AddCurrentQueuedOperation();
        }

        void IRedisQueueCompletableOperationAsync.CompleteVoidQueuedCommandAsync(Func<CancellationToken, ValueTask> voidReadCommand)
        {
            //AssertCurrentOperation();
            // this can happen when replaying pipeline/transaction
            if (CurrentQueuedOperation == null) return;

            CurrentQueuedOperation.WithAsyncReadCommand(voidReadCommand);
            AddCurrentQueuedOperation();
        }

        ValueTask IAsyncDisposable.DisposeAsync()
        {
            Dispose();
            return default;
        }

        async ValueTask IRedisPipelineSharedAsync.FlushAsync(CancellationToken token)
        {
            try
            {
                // flush send buffers
                await RedisClient.FlushSendBufferAsync(token).ConfigureAwait(false);
                RedisClient.ResetSendBuffer();

                //receive expected results
                foreach (var queuedCommand in QueuedCommands)
                {
                    await queuedCommand.ProcessResultAsync(token).ConfigureAwait(false);
                }

            }
            finally
            {
                ClosePipeline();
                await RedisClient.AddTypeIdsRegisteredDuringPipelineAsync(token).ConfigureAwait(false);
            }
        }

        void IRedisTypedQueueableOperationAsync<T>.QueueCommand(Func<IRedisTypedClientAsync<T>, ValueTask> command, Action onSuccessCallback, Action<Exception> onErrorCallback)
        {
            BeginQueuedCommand(new QueuedRedisTypedCommand<T>
            {
                OnSuccessVoidCallback = onSuccessCallback,
                OnErrorCallback = onErrorCallback
            }.WithAsyncReturnCommand(command));
            RedisAllPurposePipeline.AssertSync(command(RedisClient));
        }

        void IRedisTypedQueueableOperationAsync<T>.QueueCommand(Func<IRedisTypedClientAsync<T>, ValueTask<int>> command, Action<int> onSuccessCallback, Action<Exception> onErrorCallback)
        {
            BeginQueuedCommand(new QueuedRedisTypedCommand<T>
            {
                OnSuccessIntCallback = onSuccessCallback,
                OnErrorCallback = onErrorCallback
            }.WithAsyncReturnCommand(command));
            RedisAllPurposePipeline.AssertSync(command(RedisClient));
        }

        void IRedisTypedQueueableOperationAsync<T>.QueueCommand(Func<IRedisTypedClientAsync<T>, ValueTask<long>> command, Action<long> onSuccessCallback, Action<Exception> onErrorCallback)
        {
            BeginQueuedCommand(new QueuedRedisTypedCommand<T>
            {
                OnSuccessLongCallback = onSuccessCallback,
                OnErrorCallback = onErrorCallback
            }.WithAsyncReturnCommand(command));
            RedisAllPurposePipeline.AssertSync(command(RedisClient));
        }

        void IRedisTypedQueueableOperationAsync<T>.QueueCommand(Func<IRedisTypedClientAsync<T>, ValueTask<bool>> command, Action<bool> onSuccessCallback, Action<Exception> onErrorCallback)
        {
            BeginQueuedCommand(new QueuedRedisTypedCommand<T>
            {
                OnSuccessBoolCallback = onSuccessCallback,
                OnErrorCallback = onErrorCallback
            }.WithAsyncReturnCommand(command));
            RedisAllPurposePipeline.AssertSync(command(RedisClient));
        }

        void IRedisTypedQueueableOperationAsync<T>.QueueCommand(Func<IRedisTypedClientAsync<T>, ValueTask<double>> command, Action<double> onSuccessCallback, Action<Exception> onErrorCallback)
        {
            BeginQueuedCommand(new QueuedRedisTypedCommand<T>
            {
                OnSuccessDoubleCallback = onSuccessCallback,
                OnErrorCallback = onErrorCallback
            }.WithAsyncReturnCommand(command));
            RedisAllPurposePipeline.AssertSync(command(RedisClient));
        }

        void IRedisTypedQueueableOperationAsync<T>.QueueCommand(Func<IRedisTypedClientAsync<T>, ValueTask<byte[]>> command, Action<byte[]> onSuccessCallback, Action<Exception> onErrorCallback)
        {
            BeginQueuedCommand(new QueuedRedisTypedCommand<T>
            {
                OnSuccessBytesCallback = onSuccessCallback,
                OnErrorCallback = onErrorCallback
            }.WithAsyncReturnCommand(command));
            RedisAllPurposePipeline.AssertSync(command(RedisClient));
        }

        void IRedisTypedQueueableOperationAsync<T>.QueueCommand(Func<IRedisTypedClientAsync<T>, ValueTask<string>> command, Action<string> onSuccessCallback, Action<Exception> onErrorCallback)
        {
            BeginQueuedCommand(new QueuedRedisTypedCommand<T>
            {
                OnSuccessStringCallback = onSuccessCallback,
                OnErrorCallback = onErrorCallback
            }.WithAsyncReturnCommand(command));
            RedisAllPurposePipeline.AssertSync(command(RedisClient));
        }

        void IRedisTypedQueueableOperationAsync<T>.QueueCommand(Func<IRedisTypedClientAsync<T>, ValueTask<T>> command, Action<T> onSuccessCallback, Action<Exception> onErrorCallback)
        {
            BeginQueuedCommand(new QueuedRedisTypedCommand<T>
            {
                OnSuccessTypeCallback = x => onSuccessCallback(JsonSerializer.DeserializeFromString<T>(x)),
                OnErrorCallback = onErrorCallback
            }.WithAsyncReturnCommand(command));
            RedisAllPurposePipeline.AssertSync(command(RedisClient));
        }

        void IRedisTypedQueueableOperationAsync<T>.QueueCommand(Func<IRedisTypedClientAsync<T>, ValueTask<List<string>>> command, Action<List<string>> onSuccessCallback, Action<Exception> onErrorCallback)
        {
            BeginQueuedCommand(new QueuedRedisTypedCommand<T>
            {
                OnSuccessMultiStringCallback = onSuccessCallback,
                OnErrorCallback = onErrorCallback
            }.WithAsyncReturnCommand(command));
            RedisAllPurposePipeline.AssertSync(command(RedisClient));
        }

        void IRedisTypedQueueableOperationAsync<T>.QueueCommand(Func<IRedisTypedClientAsync<T>, ValueTask<HashSet<string>>> command, Action<HashSet<string>> onSuccessCallback, Action<Exception> onErrorCallback)
        {
            BeginQueuedCommand(new QueuedRedisTypedCommand<T>
            {
                OnSuccessMultiStringCallback = list => onSuccessCallback(list.ToSet()),
                OnErrorCallback = onErrorCallback
            }.WithAsyncReturnCommand(async r =>
            {
                var result = await command(r).ConfigureAwait(false);
                return result.ToList();
            }));
            RedisAllPurposePipeline.AssertSync(command(RedisClient));
        }

        void IRedisTypedQueueableOperationAsync<T>.QueueCommand(Func<IRedisTypedClientAsync<T>, ValueTask<List<T>>> command, Action<List<T>> onSuccessCallback, Action<Exception> onErrorCallback)
        {
            BeginQueuedCommand(new QueuedRedisTypedCommand<T>
            {
                OnSuccessMultiTypeCallback = x => onSuccessCallback(x.ConvertAll(JsonSerializer.DeserializeFromString<T>)),
                OnErrorCallback = onErrorCallback
            }.WithAsyncReturnCommand(command));
            RedisAllPurposePipeline.AssertSync(command(RedisClient));
        }

        async ValueTask<bool> IRedisPipelineSharedAsync.ReplayAsync(CancellationToken token)
        {
            RedisClient.Pipeline = this;
            // execute
            foreach (var queuedCommand in QueuedCommands)
            {
                if (queuedCommand is QueuedRedisTypedCommand<T> cmd)
                    await cmd.ExecuteAsync(RedisClient).ConfigureAwait(false);
            }
            await AsAsync().FlushAsync(token).ConfigureAwait(false);
            return true;
        }
    }
}