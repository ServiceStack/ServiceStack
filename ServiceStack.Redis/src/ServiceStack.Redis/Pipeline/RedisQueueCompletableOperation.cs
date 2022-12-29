using System;
using System.Collections.Generic;
using ServiceStack.Redis.Pipeline;

namespace ServiceStack.Redis
{
    /// <summary>
    /// Redis operation (transaction/pipeline) that allows queued commands to be completed
    /// </summary>
    public partial class RedisQueueCompletableOperation
    {
        internal readonly List<QueuedRedisOperation> QueuedCommands = new List<QueuedRedisOperation>();

        internal QueuedRedisOperation CurrentQueuedOperation;

        internal void BeginQueuedCommand(QueuedRedisOperation queuedRedisOperation)
        {
            if (CurrentQueuedOperation != null)
                throw new InvalidOperationException("The previous queued operation has not been commited");

            CurrentQueuedOperation = queuedRedisOperation;
        }

        internal void AssertCurrentOperation()
        {
            if (CurrentQueuedOperation == null)
                throw new InvalidOperationException("No queued operation is currently set");
        }

        protected virtual void AddCurrentQueuedOperation()
        {
            this.QueuedCommands.Add(CurrentQueuedOperation);
            CurrentQueuedOperation = null;
        }

        public virtual void CompleteVoidQueuedCommand(Action voidReadCommand)
        {
            //AssertCurrentOperation();
            // this can happen when replaying pipeline/transaction
            if (CurrentQueuedOperation == null) return;

            CurrentQueuedOperation.VoidReadCommand = voidReadCommand;
            AddCurrentQueuedOperation();
        }

        public virtual void CompleteIntQueuedCommand(Func<int> intReadCommand)
        {
            //AssertCurrentOperation();
            // this can happen when replaying pipeline/transaction
            if (CurrentQueuedOperation == null) return;

            CurrentQueuedOperation.IntReadCommand = intReadCommand;
            AddCurrentQueuedOperation();
        }

        public virtual void CompleteLongQueuedCommand(Func<long> longReadCommand)
        {
            //AssertCurrentOperation();
            // this can happen when replaying pipeline/transaction
            if (CurrentQueuedOperation == null) return;

            CurrentQueuedOperation.LongReadCommand = longReadCommand;
            AddCurrentQueuedOperation();
        }

        public virtual void CompleteBytesQueuedCommand(Func<byte[]> bytesReadCommand)
        {
            //AssertCurrentOperation();
            // this can happen when replaying pipeline/transaction
            if (CurrentQueuedOperation == null) return;

            CurrentQueuedOperation.BytesReadCommand = bytesReadCommand;
            AddCurrentQueuedOperation();
        }

        public virtual void CompleteMultiBytesQueuedCommand(Func<byte[][]> multiBytesReadCommand)
        {
            //AssertCurrentOperation();
            // this can happen when replaying pipeline/transaction
            if (CurrentQueuedOperation == null) return;

            CurrentQueuedOperation.MultiBytesReadCommand = multiBytesReadCommand;
            AddCurrentQueuedOperation();
        }

        public virtual void CompleteStringQueuedCommand(Func<string> stringReadCommand)
        {
            //AssertCurrentOperation();
            // this can happen when replaying pipeline/transaction
            if (CurrentQueuedOperation == null) return;

            CurrentQueuedOperation.StringReadCommand = stringReadCommand;
            AddCurrentQueuedOperation();
        }

        public virtual void CompleteMultiStringQueuedCommand(Func<List<string>> multiStringReadCommand)
        {
            //AssertCurrentOperation();
            // this can happen when replaying pipeline/transaction
            if (CurrentQueuedOperation == null) return;

            CurrentQueuedOperation.MultiStringReadCommand = multiStringReadCommand;
            AddCurrentQueuedOperation();
        }

        public virtual void CompleteDoubleQueuedCommand(Func<double> doubleReadCommand)
        {
            //AssertCurrentOperation();
            // this can happen when replaying pipeline/transaction
            if (CurrentQueuedOperation == null) return;

            CurrentQueuedOperation.DoubleReadCommand = doubleReadCommand;
            AddCurrentQueuedOperation();
        }

        public virtual void CompleteRedisDataQueuedCommand(Func<RedisData> redisDataReadCommand)
        {
            //AssertCurrentOperation();
            // this can happen when replaying pipeline/transaction
            if (CurrentQueuedOperation == null) return;

            CurrentQueuedOperation.RedisDataReadCommand = redisDataReadCommand;
            AddCurrentQueuedOperation();
        }

    }
}
