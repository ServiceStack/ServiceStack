//Copyright (c) ServiceStack, Inc. All Rights Reserved.
//License: https://raw.github.com/ServiceStack/ServiceStack/master/license.txt

using System;
using System.Collections.Generic;

namespace ServiceStack.Messaging
{
    public class MessageQueueClientFactory
        : IMessageQueueClientFactory
    {
        public IMessageQueueClient CreateMessageQueueClient()
        {
            return new InMemoryMessageQueueClient(this);
        }

        readonly object syncLock = new object();

        public event EventHandler<EventArgs> MessageReceived;

        void InvokeMessageReceived(EventArgs e)
        {
            var received = MessageReceived;
            received?.Invoke(this, e);
        }

        private readonly Dictionary<string, Queue<byte[]>> queueMessageBytesMap
            = new Dictionary<string, Queue<byte[]>>();

        public void PublishMessage<T>(string queueName, IMessage<T> message)
        {
            PublishMessage(queueName, message.ToBytes());
        }

        public void PublishMessage(string queueName, byte[] messageBytes)
        {
            lock (syncLock)
            {
                if (!queueMessageBytesMap.TryGetValue(queueName, out var bytesQueue))
                {
                    bytesQueue = new Queue<byte[]>();
                    queueMessageBytesMap[queueName] = bytesQueue;
                }

                bytesQueue.Enqueue(messageBytes);
            }

            InvokeMessageReceived(new EventArgs());
        }

        /// <summary>
        /// Returns the next message from queueName or null if no message
        /// </summary>
        /// <param name="queueName"></param>
        /// <returns></returns>
        public byte[] GetMessageAsync(string queueName)
        {
            lock (syncLock)
            {
                if (!queueMessageBytesMap.TryGetValue(queueName, out var bytesQueue))
                    return null;

                if (bytesQueue.Count == 0)
                    return null;

                var messageBytes = bytesQueue.Dequeue();
                return messageBytes;
            }
        }

        public void Dispose()
        {
        }
    }
}