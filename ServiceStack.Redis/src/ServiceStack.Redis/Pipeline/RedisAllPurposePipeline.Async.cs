using ServiceStack.Redis.Pipeline;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ServiceStack.Redis;

public partial class RedisAllPurposePipeline : IRedisPipelineAsync
{
    private IRedisPipelineAsync AsAsync() => this;

    private protected virtual async ValueTask<bool> ReplayAsync(CancellationToken token)
    {
        Init();
        await ExecuteAsync().ConfigureAwait(false);
        await AsAsync().FlushAsync(token).ConfigureAwait(false);
        return true;
    }

    protected async ValueTask ExecuteAsync()
    {
        int count = QueuedCommands.Count;
        for (int i = 0; i < count; ++i)
        {
            var op = QueuedCommands[0];
            QueuedCommands.RemoveAt(0);
            await op.ExecuteAsync(RedisClient).ConfigureAwait(false);
            QueuedCommands.Add(op);
        }
    }

    ValueTask<bool> IRedisPipelineSharedAsync.ReplayAsync(CancellationToken token)
        => ReplayAsync(token);

    async ValueTask IRedisPipelineSharedAsync.FlushAsync(CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        using var cancellation = RedisClient.CloseSocketOnCancellation(token);
        try
        {
            using (var send = CancellationTokenSource.CreateLinkedTokenSource(token))
            {
                if (RedisClient.SendTimeout > 0) send.CancelAfter(RedisClient.SendTimeout);
                using var close = RedisClient.CloseSocketOnCancellation(send.Token);
                try { await RedisClient.FlushSendBufferAsync(send.Token).ConfigureAwait(false); }
                catch (Exception ex) when (send.IsCancellationRequested)
                {
                    token.ThrowIfCancellationRequested();
                    throw new TimeoutException($"Redis pipeline send exceeded SendTimeout {RedisClient.SendTimeout}ms.", ex);
                }
            }
            RedisClient.ResetSendBuffer();
            // One linked source and socket-close registration per flush; re-arming CancelAfter restarts the
            // ReceiveTimeout for each reply without allocating per queued command.
            using var receive = CancellationTokenSource.CreateLinkedTokenSource(token);
            using var closeOnReceiveCancel = RedisClient.CloseSocketOnCancellation(receive.Token);
            foreach (var queuedCommand in QueuedCommands)
            {
                if (RedisClient.ReceiveTimeout > 0) receive.CancelAfter(RedisClient.ReceiveTimeout);
                try
                {
                    await queuedCommand.ProcessResultAsync(receive.Token).ConfigureAwait(false);
                    receive.Token.ThrowIfCancellationRequested();
                }
                catch (Exception ex) when (receive.IsCancellationRequested)
                {
                    token.ThrowIfCancellationRequested();
                    throw new TimeoutException($"Redis pipeline receive exceeded ReceiveTimeout {RedisClient.ReceiveTimeout}ms.", ex);
                }
            }
            token.ThrowIfCancellationRequested();
        }
        catch (Exception)
        {
            // The connection cannot be reused anymore. All queued commands have been sent to redis. Even if a new command is executed, the next response read from the
            // network stream can be the response of one of the queued commands, depending on when the exception occurred. This response would be invalid for the new command.
            RedisClient.DeactivatedAt = DateTime.UtcNow;
            RedisClient.AbortConnection();
            token.ThrowIfCancellationRequested();
            throw;
        }

        ClosePipeline();
    }

    private protected virtual ValueTask DisposeAsync()
    {
        // don't need to send anything; just clean up
        Dispose();
        return default;
    }

    ValueTask IAsyncDisposable.DisposeAsync() => DisposeAsync();

    internal static void AssertSync<T>(ValueTask<T> command)
    {
        if (!command.IsCompleted)
        {
            _ = ObserveAsync(command.AsTask());
            throw new InvalidOperationException($"The operations provided to {nameof(IRedisQueueableOperationAsync.QueueCommand)} should not perform asynchronous operations internally");
        }
        // this serves two purposes: 1) surface any fault, and
        // 2) ensure that if pooled (IValueTaskSource), it is reclaimed
        _ = command.Result;
    }

    internal static void AssertSync(ValueTask command)
    {
        if (!command.IsCompleted)
        {
            _ = ObserveAsync(command.AsTask());
            throw new InvalidOperationException($"The operations provided to {nameof(IRedisQueueableOperationAsync.QueueCommand)} should not perform asynchronous operations internally");
        }
        // this serves two purposes: 1) surface any fault, and
        // 2) ensure that if pooled (IValueTaskSource), it is reclaimed
        command.GetAwaiter().GetResult();
    }

    static async Task ObserveAsync(Task task) // semantically this is "async void", but: some sync-contexts explode on that
    {
        // we've already thrown an exception via AssertSync; this
        // just ensures that an "unobserved exception" doesn't fire
        // as well
        try { await task.ConfigureAwait(false); }
        catch { }
    }

    void IRedisQueueableOperationAsync.QueueCommand(Func<IRedisClientAsync, ValueTask> command, Action onSuccessCallback, Action<Exception> onErrorCallback)
    {
        BeginQueuedCommand(new QueuedRedisCommand
        {
            OnSuccessVoidCallback = onSuccessCallback,
            OnErrorCallback = onErrorCallback
        }.WithAsyncReturnCommand(command));
        AssertSync(command(RedisClient));
    }

    void IRedisQueueableOperationAsync.QueueCommand(Func<IRedisClientAsync, ValueTask<int>> command, Action<int> onSuccessCallback, Action<Exception> onErrorCallback)
    {
        BeginQueuedCommand(new QueuedRedisCommand
        {
            OnSuccessIntCallback = onSuccessCallback,
            OnErrorCallback = onErrorCallback
        }.WithAsyncReturnCommand(command));
        AssertSync(command(RedisClient));
    }

    void IRedisQueueableOperationAsync.QueueCommand(Func<IRedisClientAsync, ValueTask<long>> command, Action<long> onSuccessCallback, Action<Exception> onErrorCallback)
    {
        BeginQueuedCommand(new QueuedRedisCommand
        {
            OnSuccessLongCallback = onSuccessCallback,
            OnErrorCallback = onErrorCallback
        }.WithAsyncReturnCommand(command));
        AssertSync(command(RedisClient));
    }

    void IRedisQueueableOperationAsync.QueueCommand(Func<IRedisClientAsync, ValueTask<bool>> command, Action<bool> onSuccessCallback, Action<Exception> onErrorCallback)
    {
        BeginQueuedCommand(new QueuedRedisCommand
        {
            OnSuccessBoolCallback = onSuccessCallback,
            OnErrorCallback = onErrorCallback
        }.WithAsyncReturnCommand(command));
        AssertSync(command(RedisClient));
    }

    void IRedisQueueableOperationAsync.QueueCommand(Func<IRedisClientAsync, ValueTask<double>> command, Action<double> onSuccessCallback, Action<Exception> onErrorCallback)
    {
        BeginQueuedCommand(new QueuedRedisCommand
        {
            OnSuccessDoubleCallback = onSuccessCallback,
            OnErrorCallback = onErrorCallback
        }.WithAsyncReturnCommand(command));
        AssertSync(command(RedisClient));
    }

    void IRedisQueueableOperationAsync.QueueCommand(Func<IRedisClientAsync, ValueTask<byte[]>> command, Action<byte[]> onSuccessCallback, Action<Exception> onErrorCallback)
    {
        BeginQueuedCommand(new QueuedRedisCommand
        {
            OnSuccessBytesCallback = onSuccessCallback,
            OnErrorCallback = onErrorCallback
        }.WithAsyncReturnCommand(command));
        AssertSync(command(RedisClient));
    }

    void IRedisQueueableOperationAsync.QueueCommand(Func<IRedisClientAsync, ValueTask<byte[][]>> command, Action<byte[][]> onSuccessCallback, Action<Exception> onErrorCallback)
    {
        BeginQueuedCommand(new QueuedRedisCommand
        {
            OnSuccessMultiBytesCallback = onSuccessCallback,
            OnErrorCallback = onErrorCallback
        }.WithAsyncReturnCommand(command));
        AssertSync(command(RedisClient));
    }

    void IRedisQueueableOperationAsync.QueueCommand(Func<IRedisClientAsync, ValueTask<string>> command, Action<string> onSuccessCallback, Action<Exception> onErrorCallback)
    {
        BeginQueuedCommand(new QueuedRedisCommand
        {
            OnSuccessStringCallback = onSuccessCallback,
            OnErrorCallback = onErrorCallback
        }.WithAsyncReturnCommand(command));
        AssertSync(command(RedisClient));
    }

    void IRedisQueueableOperationAsync.QueueCommand(Func<IRedisClientAsync, ValueTask<List<string>>> command, Action<List<string>> onSuccessCallback, Action<Exception> onErrorCallback)
    {
        BeginQueuedCommand(new QueuedRedisCommand
        {
            OnSuccessMultiStringCallback = onSuccessCallback,
            OnErrorCallback = onErrorCallback
        }.WithAsyncReturnCommand(command));
        AssertSync(command(RedisClient));
    }

    void IRedisQueueableOperationAsync.QueueCommand(Func<IRedisClientAsync, ValueTask<HashSet<string>>> command, Action<HashSet<string>> onSuccessCallback, Action<Exception> onErrorCallback)
    {
        BeginQueuedCommand(new QueuedRedisCommand
        {
            OnSuccessMultiStringCallback = list => onSuccessCallback(list.ToSet()),
            OnErrorCallback = onErrorCallback
        }.WithAsyncReturnCommand(async r =>
        {
            var result = await command(r).ConfigureAwait(false);
            return result.ToList();
        }));
        AssertSync(command(RedisClient));
    }

    void IRedisQueueableOperationAsync.QueueCommand(Func<IRedisClientAsync, ValueTask<Dictionary<string, string>>> command, Action<Dictionary<string, string>> onSuccessCallback, Action<Exception> onErrorCallback)
    {
        BeginQueuedCommand(new QueuedRedisCommand
        {
            OnSuccessDictionaryStringCallback = onSuccessCallback,
            OnErrorCallback = onErrorCallback
        }.WithAsyncReturnCommand(command));
        AssertSync(command(RedisClient));
    }

    void IRedisQueueableOperationAsync.QueueCommand(Func<IRedisClientAsync, ValueTask<RedisData>> command, Action<RedisData> onSuccessCallback, Action<Exception> onErrorCallback)
    {
        BeginQueuedCommand(new QueuedRedisCommand
        {
            OnSuccessRedisDataCallback = onSuccessCallback,
            OnErrorCallback = onErrorCallback
        }.WithAsyncReturnCommand(command));
        AssertSync(command(RedisClient));
    }

    void IRedisQueueableOperationAsync.QueueCommand(Func<IRedisClientAsync, ValueTask<RedisText>> command, Action<RedisText> onSuccessCallback, Action<Exception> onErrorCallback)
    {
        BeginQueuedCommand(new QueuedRedisCommand
        {
            OnSuccessRedisTextCallback = onSuccessCallback,
            OnErrorCallback = onErrorCallback
        }.WithAsyncReturnCommand(command));
        AssertSync(command(RedisClient));
    }

    void IRedisQueueCompletableOperationAsync.CompleteMultiBytesQueuedCommandAsync(Func<CancellationToken, ValueTask<byte[][]>> multiBytesReadCommand)
    {
        //AssertCurrentOperation();
        // this can happen when replaying pipeline/transaction
        if (CurrentQueuedOperation == null) return;

        CurrentQueuedOperation.WithAsyncReadCommand(multiBytesReadCommand);
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

    void IRedisQueueCompletableOperationAsync.CompleteBytesQueuedCommandAsync(Func<CancellationToken, ValueTask<byte[]>> bytesReadCommand)
    {
        //AssertCurrentOperation();
        // this can happen when replaying pipeline/transaction
        if (CurrentQueuedOperation == null) return;

        CurrentQueuedOperation.WithAsyncReadCommand(bytesReadCommand);
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

    void IRedisQueueCompletableOperationAsync.CompleteStringQueuedCommandAsync(Func<CancellationToken, ValueTask<string>> stringReadCommand)
    {
        //AssertCurrentOperation();
        // this can happen when replaying pipeline/transaction
        if (CurrentQueuedOperation == null) return;

        CurrentQueuedOperation.WithAsyncReadCommand(stringReadCommand);
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
}