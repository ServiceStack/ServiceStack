using System;
using System.Collections.Concurrent;
using System.Threading;
using ServiceStack.Aws.Support;

namespace ServiceStack.Aws.Sqs
{
    public class SqsMqBufferFactory : ISqsMqBufferFactory
    {
        private readonly SqsConnectionFactory sqsConnectionFactory;
        private static readonly ConcurrentDictionary<string, ISqsMqBuffer> queueNameBuffers = new ConcurrentDictionary<string, ISqsMqBuffer>();
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
                bufferFlushIntervalSeconds = value > 0
                    ? value
                    : 0;

                if (timer != null)
                    return;

                timer = new Timer(OnTimerElapsed, null, bufferFlushIntervalSeconds, Timeout.Infinite);
            }
        }

        private void OnTimerElapsed(object state)
        {
            if (Interlocked.CompareExchange(ref processingTimer, 1, 0) > 0)
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
                if (bufferFlushIntervalSeconds <= 0)
                {
                    timer.Dispose();
                    timer = null;
                }
                else
                {
                    timer.Change(bufferFlushIntervalSeconds, Timeout.Infinite);
                }

                Interlocked.CompareExchange(ref processingTimer, 0, 1);
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
            foreach (var buffer in queueNameBuffers)
            {
                buffer.Value.Dispose();
            }
        }
    }
}