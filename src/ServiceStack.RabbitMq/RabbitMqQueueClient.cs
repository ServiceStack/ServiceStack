using System;
using System.Threading;
using RabbitMQ.Client;
using ServiceStack.Messaging;

namespace ServiceStack.RabbitMq
{
    public class RabbitMqQueueClient : RabbitMqProducer, IMessageQueueClient
    {
        public RabbitMqQueueClient(RabbitMqMessageFactory msgFactory)
            : base(msgFactory) {}

        public void Notify(string queueName, IMessage message)
        {
            if (!declaredQueues.Contains(queueName))
            {
                Channel.RegisterTopic(queueName);
                declaredQueues.Add(queueName);
            }

            var messageBytes = message.Body.ToJson().ToUtf8Bytes();

            Channel.BasicPublish(QueueNames.ExchangeTopic,
                routingKey: queueName,
                basicProperties: null, body: messageBytes);
        }

        public IMessage<T> Get<T>(string queueName, TimeSpan? timeOut)
        {
            if (!declaredQueues.Contains(queueName))
            {
                Channel.RegisterQueue(queueName);
                declaredQueues.Add(queueName);
            }

            var now = DateTime.UtcNow;

            while (timeOut == null || (DateTime.Now - now) < timeOut.Value)
            {
                var basicMsg = Channel.BasicGet(queueName, noAck:false);
                if (basicMsg != null)
                {
                    return basicMsg.ToMessage<T>();
                }
                Thread.Sleep(100);
            }

            return null;
        }

        public IMessage<T> GetAsync<T>(string queueName)
        {
            if (!declaredQueues.Contains(queueName))
            {
                Channel.RegisterQueue(queueName);
                declaredQueues.Add(queueName);
            }

            var basicMsg = Channel.BasicGet(queueName, noAck:false);
            return basicMsg.ToMessage<T>();
        }

        public void Ack(IMessage message)
        {
            var deliveryTag = ulong.Parse(message.Tag);
            Channel.BasicAck(deliveryTag, multiple:false);
        }

        public void Nak(IMessage message, bool requeue)
        {
            var deliveryTag = ulong.Parse(message.Tag);
            Channel.BasicNack(deliveryTag, multiple: false, requeue: requeue);
        }

        public IMessage<T> CreateMessage<T>(object mqResponse)
        {
            var msgResult = mqResponse as BasicGetResult;
            if (msgResult != null)
            {
                return msgResult.ToMessage<T>();
            }

            return (IMessage<T>) mqResponse;
        }
    }
}