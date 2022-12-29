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
using System.Threading;
using System.Threading.Tasks;
using ServiceStack.Redis.Pipeline;

namespace ServiceStack.Redis
{
    /// <summary>
    /// Adds support for Redis Transactions (i.e. MULTI/EXEC/DISCARD operations).
    /// </summary>
    public partial class RedisTransaction
        : IRedisTransactionAsync, IRedisQueueCompletableOperationAsync
    {
        /// <summary>
        /// Issue exec command (not queued)
        /// </summary>
        private async ValueTask ExecAsync(CancellationToken token)
        {
            RedisClient.Exec();
            await RedisClient.FlushSendBufferAsync(token).ConfigureAwait(false);
            RedisClient.ResetSendBuffer();
        }


        /// <summary>
        /// Put "QUEUED" messages at back of queue
        /// </summary>
        partial void QueueExpectQueuedAsync()
        {
            QueuedCommands.Insert(0, new QueuedRedisOperation
            {
            }.WithAsyncReadCommand(RedisClient.ExpectQueuedAsync));
        }

        async ValueTask<bool> IRedisTransactionAsync.CommitAsync(CancellationToken token)
        {
            bool rc = true;
            try
            {
                numCommands = QueuedCommands.Count / 2;

                //insert multi command at beginning
                QueuedCommands.Insert(0, new QueuedRedisCommand
                {
                }.WithAsyncReturnCommand(VoidReturnCommandAsync: r => { Init(); return default; })
                .WithAsyncReadCommand(RedisClient.ExpectOkAsync));

                //the first half of the responses will be "QUEUED",
                // so insert reading of multiline after these responses
                QueuedCommands.Insert(numCommands + 1, new QueuedRedisOperation
                {
                    OnSuccessIntCallback = handleMultiDataResultCount
                }.WithAsyncReadCommand(RedisClient.ReadMultiDataResultCountAsync));

                // add Exec command at end (not queued)
                QueuedCommands.Add(new RedisCommand
                {
                }.WithAsyncReturnCommand(r => ExecAsync(token)));

                //execute transaction
                await ExecAsync(token).ConfigureAwait(false);

                //receive expected results
                foreach (var queuedCommand in QueuedCommands)
                {
                    await queuedCommand.ProcessResultAsync(token).ConfigureAwait(false);
                }
            }
            catch (RedisTransactionFailedException)
            {
                rc = false;
            }
            finally
            {
                RedisClient.Transaction = null;
                ClosePipeline();
                await RedisClient.AddTypeIdsRegisteredDuringPipelineAsync(token).ConfigureAwait(false);
            }
            return rc;
        }

        ValueTask IRedisTransactionAsync.RollbackAsync(CancellationToken token)
        {
            Rollback(); // not currently anything different to do on the async path
            return default;
        }
        // note: this also means that Dispose doesn't need to be complex; if Rollback needed
        // splitting, we would need to override DisposeAsync and split the code, too


        private protected override async ValueTask<bool> ReplayAsync(CancellationToken token)
        {
            bool rc = true;
            try
            {
                await ExecuteAsync().ConfigureAwait(false);

                //receive expected results
                foreach (var queuedCommand in QueuedCommands)
                {
                    await queuedCommand.ProcessResultAsync(token).ConfigureAwait(false);
                }
            }
            catch (RedisTransactionFailedException)
            {
                rc = false;
            }
            finally
            {
                RedisClient.Transaction = null;
                ClosePipeline();
                await RedisClient.AddTypeIdsRegisteredDuringPipelineAsync(token).ConfigureAwait(false);
            }
            return rc;
        }
    }
}