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
            using (__requestAccess())
            {
                var json = message.Body.ToJson();
                var messageBytes = json.ToUtf8Bytes();

                PublishMessage(QueueNames.ExchangeTopic,
                    routingKey: queueName,
                    basicProperties: null, body: messageBytes);
            }
        }

        public IMessage<T> Get<T>(string queueName, TimeSpan? timeOut = null)
        {
            using (__requestAccess())
            {
                var now = DateTime.UtcNow;

                while (timeOut == null || (DateTime.UtcNow - now) < timeOut.Value)
                {
                    var basicMsg = GetMessage(queueName, noAck: false);
                    if (basicMsg != null)
                    {
                        return basicMsg.ToMessage<T>();
                    }
                    Thread.Sleep(100);
                }

                return null;
            }
        }

        public IMessage<T> GetAsync<T>(string queueName)
        {
            using (__requestAccess())
            {
                var basicMsg = GetMessage(queueName, noAck: false);
                return basicMsg.ToMessage<T>();
            }
        }

        public void Ack(IMessage message)
        {
            var deliveryTag = ulong.Parse(message.Tag);
            Channel.BasicAck(deliveryTag, multiple:false);
        }

        public void Nak(IMessage message, bool requeue, Exception exception = null)
        {
            try
            {
                if (requeue)
                {
                    var deliveryTag = ulong.Parse(message.Tag);
                    Channel.BasicNack(deliveryTag, multiple: false, requeue: requeue);
                }
                else
                {
                    Publish(message.ToDlqQueueName(), message, QueueNames.ExchangeDlq);
                    Ack(message);
                }
            }
            catch (Exception)
            {
                var deliveryTag = ulong.Parse(message.Tag);
                Channel.BasicNack(deliveryTag, multiple: false, requeue: requeue);
            }
        }

        public IMessage<T> CreateMessage<T>(object mqResponse)
        {
            using (__requestAccess())
            {
                var msgResult = mqResponse as BasicGetResult;
                if (msgResult != null)
                {
                    return msgResult.ToMessage<T>();
                }

                return (IMessage<T>)mqResponse;
            }
        }

        public string GetTempQueueName()
        {
            var anonMq = Channel.QueueDeclare(
                queue: QueueNames.GetTempQueueName(),
                durable:false,
                exclusive:true,
                autoDelete:true,
                arguments:null);

            return anonMq.QueueName;
        }
    }
}