using System;

namespace ServiceStack.Messaging
{
    public interface IMessageQueueClient
        : IMessageProducer
    {
        /// <summary>
        /// Publish the specified message into the durable queue @queueName
        /// </summary>
        void Publish(string queueName, IMessage message);

        /// <summary>
        /// Publish the specified message into the transient queue @queueName
        /// </summary>
        void Notify(string queueName, IMessage message);

        /// <summary>
        /// Synchronous blocking get.
        /// </summary>
        IMessage<T> Get<T>(string queueName, TimeSpan? timeOut=null);

        /// <summary>
        /// Non blocking get message
        /// </summary>
        IMessage<T> GetAsync<T>(string queueName);

        /// <summary>
        /// Acknowledge the message has been successfully received or processed
        /// </summary>
        void Ack(IMessage message);

        /// <summary>
        /// Negative acknowledgement the message was not processed correctly
        /// </summary>
        void Nak(IMessage message, bool requeue, Exception exception = null);

        /// <summary>
        /// Create a typed message from a raw MQ Response artefact
        /// </summary>
        IMessage<T> CreateMessage<T>(object mqResponse);

        /// <summary>
        /// Create a temporary Queue for Request / Reply
        /// </summary>
        /// <returns></returns>
        string GetTempQueueName();
    }
}