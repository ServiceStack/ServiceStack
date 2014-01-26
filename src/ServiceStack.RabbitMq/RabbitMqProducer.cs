using System;
using System.Collections.Generic;
using RabbitMQ.Client;
using ServiceStack.Logging;
using ServiceStack.Messaging;

namespace ServiceStack.RabbitMq
{
    public class RabbitMqProducer : IMessageProducer
    {
        public static ILog Log = LogManager.GetLogger(typeof(RabbitMqProducer));
        protected readonly RabbitMqMessageFactory msgFactory;
        public int RetryCount { get; set; }
        public Action OnPublishedCallback { get; set; }
        protected HashSet<string> declaredQueues = new HashSet<string>();

        private IConnection connection;
        public IConnection Connection
        {
            get
            {
                if (connection == null)
                {
                    connection = msgFactory.ConnectionFactory.CreateConnection();
                }
                return connection;
            }
        }

        private IModel channel;
        public IModel Channel
        {
            get
            {
                if (channel == null)
                {
                    channel = Connection.OpenChannel();
                    //http://www.rabbitmq.com/blog/2012/04/25/rabbitmq-performance-measurements-part-2/
                    //http://www.rabbitmq.com/amqp-0-9-1-reference.html
                    channel.BasicQos(prefetchCount: 20, prefetchSize: 0, global: false);
                }
                return channel;
            }
        }

        public RabbitMqProducer(RabbitMqMessageFactory msgFactory)
        {
            this.msgFactory = msgFactory;
        }

        public void Publish<T>(T messageBody)
        {
            var message = typeof(IMessage).IsAssignableFromType(typeof(T))
                ? (IMessage<T>)messageBody
                : new Message<T>(messageBody);

            Publish(message);
        }
 
        public void Publish<T>(IMessage<T> message)
        {
            Publish(message.ToInQueueName(), message);
        }

        public void Publish(string queueName, IMessage message)
        {
            if (!declaredQueues.Contains(queueName))
            {
                Channel.RegisterQueue(queueName);
                declaredQueues.Add(queueName);
            }

            var props = Channel.CreateBasicProperties();
            props.SetPersistent(true);
            props.PopulateFromMessage(message);

            var messageBytes = message.Body.ToJson().ToUtf8Bytes();

            Channel.BasicPublish(QueueNames.Exchange,
                routingKey: queueName,
                basicProperties: props, body: messageBytes);

            if (OnPublishedCallback != null)
            {
                OnPublishedCallback();
            }
        }

        public virtual void Dispose()
        {
            declaredQueues = new HashSet<string>();
            if (channel != null)
            {
                try
                {
                    channel.Dispose();
                }
                catch (Exception ex)
                {
                    Log.Error("Error trying to dispose RabbitMqProducer model", ex);
                } 
                channel = null;
            }
            if (connection != null)
            {
                try
                {
                    connection.Dispose();
                }
                catch (Exception ex)
                {
                    Log.Error("Error trying to dispose RabbitMqProducer connection", ex);
                }
                connection = null;
            }
        }
    }
}