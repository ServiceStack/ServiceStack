using ServiceStack.Redis.Pipeline;
using System;

namespace ServiceStack.Redis
{

    public partial class RedisAllPurposePipeline : RedisCommandQueue, IRedisPipeline
    {
        /// <summary>
        /// General purpose pipeline
        /// </summary>
        /// <param name="redisClient"></param>
        public RedisAllPurposePipeline(RedisClient redisClient)
            : base(redisClient)
        {
            Init();

        }

        protected virtual void Init()
        {
            if (RedisClient.Transaction != null)
                throw new InvalidOperationException("A transaction is already in use");

            if (RedisClient.Pipeline != null)
                throw new InvalidOperationException("A pipeline is already in use");

            RedisClient.Pipeline = this;
        }

        /// <summary>
        /// Flush send buffer, and read responses
        /// </summary>
        public void Flush()
        {
            // flush send buffers
            RedisClient.FlushAndResetSendBuffer();
            
            try
            {
                //receive expected results
                foreach (var queuedCommand in QueuedCommands)
                {
                    queuedCommand.ProcessResult();
                }
            }
            catch (Exception)
            {
                // The connection cannot be reused anymore. All queued commands have been sent to redis. Even if a new command is executed, the next response read from the
                // network stream can be the response of one of the queued commands, depending on when the exception occurred. This response would be invalid for the new command.
                RedisClient.DisposeConnection();
                throw;
            }
            
            ClosePipeline();
        }

        protected void Execute()
        {
            int count = QueuedCommands.Count;
            for (int i = 0; i < count; ++i)
            {
                var op = QueuedCommands[0];
                QueuedCommands.RemoveAt(0);
                op.Execute(RedisClient);
                QueuedCommands.Add(op);
            }
        }

        public virtual bool Replay()
        {
            Init();
            Execute();
            Flush();
            return true;
        }

        protected void ClosePipeline()
        {
            RedisClient.EndPipeline();
        }

        public virtual void Dispose()
        {
            ClosePipeline();
        }
    }
}