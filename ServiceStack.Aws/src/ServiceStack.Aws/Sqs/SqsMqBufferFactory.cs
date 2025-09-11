using System;
using System.Collections.Concurrent;
using System.Threading;
using ServiceStack.Aws.Support;

namespace ServiceStack.Aws.Sqs
{
    public class SqsMqBufferFactory : ISqsMqBufferFactory
    {
        private readonly SqsConnectionFactory sqsConnectionFactory;

        private static readonly ConcurrentDictionary<string, ISqsMqBuffer> queueNameBuffers = new();

        private int disposing;
        private Timer timer;
        private int processingTimer = 0;

        public SqsMqBufferFactory(SqsConnectionFactory sqsConnectionFactory)
        {
            Guard.AgainstNullArgument(sqsConnectionFactory, "sqsConnectionFactory");

            this.sqsConnectionFactory = sqsConnectionFactory;
        }

        public Action<Exception> ErrorHandler { get; set; }

        private int bufferFlushIntervalSeconds = 0;

        public int BufferFlushIntervalSeconds
        {
            get { return bufferFlushIntervalSeconds; }
            set
            {
                if (disposing > 0)
                {
                    return;
                }

                bufferFlushIntervalSeconds = value > 0
                    ? value
                    : 0;

                if (timer != null)
                {
                    timer.Change(bufferFlushIntervalSeconds * 1000, Timeout.Infinite);
                }
                else
                {
                    timer = new Timer(OnTimerElapsed, null, bufferFlushIntervalSeconds * 1000, Timeout.Infinite);
                }
            }
        }

        private void OnTimerElapsed(object state)
        {
            if (disposing > 0 || Interlocked.CompareExchange(ref processingTimer, 1, 0) > 0)
                return;
            try
            {
                foreach (var buffer in queueNameBuffers)
                {
                    buffer.Value.Drain(fullDrain: false);
                }
            }
            finally
            {
                Interlocked.CompareExchange(ref processingTimer, 0, 1);
                if (disposing == 0)
                {
                    timer.Change(bufferFlushIntervalSeconds * 1000, Timeout.Infinite);
                }
            }
        }

        public ISqsMqBuffer GetOrCreate(SqsQueueDefinition queueDefinition)
        {
            var queueName = queueDefinition.QueueName;

            if (queueNameBuffers.TryGetValue(queueName, out var buffer))
                return buffer;

            buffer = queueDefinition.DisableBuffering
                ? (ISqsMqBuffer)new SqsMqBufferNonBuffered(queueDefinition, sqsConnectionFactory)
                : (ISqsMqBuffer)new SqsMqBuffer(queueDefinition, sqsConnectionFactory);

            buffer.ErrorHandler = ErrorHandler;

            queueNameBuffers.TryAdd(queueName, buffer);

            return queueNameBuffers[queueName];
        }

        public void Dispose()
        {
            if (Interlocked.CompareExchange(ref disposing, 1, 0) > 0)
            {
                return;
            }

            timer?.Dispose();
            // Remove and dispose all the buffers
            foreach (var buffer in queueNameBuffers)
            {
                if (queueNameBuffers.TryRemove(buffer.Key, out var b))
                {
                    b.Dispose();
                }
            }
            timer = null;            
        }
    }
}
