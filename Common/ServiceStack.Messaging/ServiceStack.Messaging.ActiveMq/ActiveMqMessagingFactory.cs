using System;
using ServiceStack.Logging;
using ServiceStack.Messaging.ActiveMq.Support.Config;
using ServiceStack.Messaging.ActiveMq.Support.Wrappers;

namespace ServiceStack.Messaging.ActiveMq
{
    /// <summary>
    /// Creates resources that lets you communicate with ActiveMQ channels.
    /// </summary>
    public class ActiveMqMessagingFactory : IMessagingFactory
    {
        private FailoverUri failoverUri;
        private string connString;
        private readonly ILog log;

        /// <summary>
        /// Initializes a new instance of the <see cref="ActiveMqMessagingFactory"/> class.
        /// </summary>
        public ActiveMqMessagingFactory()
        {
            log = LogManager.GetLogger(GetType());
        }

        /// <summary>
        /// Gets or sets the connection string.
        /// </summary>
        /// <value>The connection string.</value>
        public string ConnectionString
        {
            get { return connString; }
            set { connString = value; }
        }

        /// <summary>
        /// Gets or sets the failover URI.
        /// </summary>
        /// <value>The failover URI.</value>
        public FailoverUri FailoverUri
        {
            get { return failoverUri; }
            set { failoverUri = value; }
        }

        /// <summary>
        /// Creates the connection.
        /// </summary>
        /// <param name="connectionString">The connection string.</param>
        /// <returns></returns>
        public IConnection CreateConnection(string connectionString)
        {
            log.Debug("Creating new Connection.");
            FailoverSettings failoverSettings = failoverUri != null ? failoverUri.FailoverSettings : null;
            return new ActiveMqConnection(this, new NmsConnectionFactoryWrapper(connectionString), failoverSettings);
        }

        /// <summary>
        /// Creates the connection using the ConnectionString property
        /// </summary>
        /// <returns></returns>
        public IConnection CreateConnection()
        {
            if (string.IsNullOrEmpty(ConnectionString))
            {
                throw new ArgumentNullException("ConnectionString", "ConnectionString has not been specified");
            }
            return CreateConnection(ConnectionString);
        }

        /// <summary>
        /// Creates the service host.
        /// </summary>
        /// <param name="listener">The listener.</param>
        /// <param name="config">The config.</param>
        /// <returns></returns>
        public IServiceHost CreateServiceHost(IGatewayListener listener, IServiceHostConfig config)
        {
            log.Debug("Creating new ServiceHost.");
            return new ActiveMqServiceHost(listener, config);
        }
    }
}
