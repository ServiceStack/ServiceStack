using System;

namespace ServiceStack.Messaging
{
    public class InMemoryMessageQueueClient
        : IMessageQueueClient
    {
        private readonly MessageQueueClientFactory factory;

        public InMemoryMessageQueueClient(MessageQueueClientFactory factory)
        {
            this.factory = factory;
        }

        public void Publish<T>(T messageBody)
        {
            factory.PublishMessage(QueueNames<T>.In, new Message<T>(messageBody));
        }

        public void Publish<T>(IMessage<T> message)
        {
            factory.PublishMessage(QueueNames<T>.In, message);
        }

        public void Publish(string queueName, byte[] messageBytes)
        {
            factory.PublishMessage(queueName, messageBytes);
        }

        public void Notify(string queueName, byte[] messageBytes)
        {
            factory.PublishMessage(queueName, messageBytes);
        }

        public byte[] GetAsync(string queueName)
        {
            return factory.GetMessageAsync(queueName);
        }

        public string WaitForNotifyOnAny(params string[] channelNames)
        {
            throw new NotImplementedException();
        }

        public byte[] Get(string queueName, TimeSpan? timeOut)
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
        }
    }
}