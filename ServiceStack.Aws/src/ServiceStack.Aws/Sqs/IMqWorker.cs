using System;
using ServiceStack.Messaging;

namespace ServiceStack.Aws.Sqs
{
    public interface IMqWorker<out T> : IDisposable
    {
        T Clone();
        void Start();
        void Stop();
        string QueueName { get; set; }
        IMessageHandlerStats GetStats();
    }
}