using System;

namespace ServiceStack.Aws.Sqs
{
    public interface ISqsMqBufferFactory : IDisposable
    {
        ISqsMqBuffer GetOrCreate(SqsQueueDefinition queueDefinition);
        int BufferFlushIntervalSeconds { get; set; }
        Action<Exception> ErrorHandler { get; set; }
    }
}