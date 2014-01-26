using System;
using RabbitMQ.Client;
using ServiceStack.Messaging;

namespace ServiceStack.RabbitMq
{
    public class RabbitMqMessageFactory : IMessageFactory
    {
        public ConnectionFactory ConnectionFactory { get; private set; }

        private int retryCount;
        public int RetryCount
        {
            get { return retryCount; }
            set
            {
                if (value < 0 || value > 1)
                    throw new ArgumentOutOfRangeException("RetryCount", 
                        "Rabbit MQ RetryCount must be 0-1");

                retryCount = value;
            }
        }

        public bool UsePolling { get; set; }

        public RabbitMqMessageFactory(string connectionString,
            string username = null, string password = null)
        {
            if (connectionString == null)
                throw new ArgumentNullException("connectionString");

            ConnectionFactory = new ConnectionFactory();

            if (username != null)
                ConnectionFactory.UserName = username;
            if (password != null)
                ConnectionFactory.Password = password;

            if (connectionString.StartsWith("amqp://"))
            {
                ConnectionFactory.Uri = connectionString;
            }
            else
            {
                var parts = connectionString.SplitOnFirst(':');
                var hostName = parts[0];
                ConnectionFactory.HostName = hostName;

                if (parts.Length > 1)
                {
                    ConnectionFactory.Port = parts[1].ToInt();
                }
            }
        }

        public RabbitMqMessageFactory(ConnectionFactory connectionFactory)
        {
            ConnectionFactory = connectionFactory;
        }

        public IMessageQueueClient CreateMessageQueueClient()
        {
            return new RabbitMqQueueClient(this) {
                RetryCount = RetryCount,
            };
        }

        public IMessageProducer CreateMessageProducer()
        {
            return new RabbitMqProducer(this) {
                RetryCount = RetryCount,
            };
        }

        public void Dispose()
        {
        }
    }
}