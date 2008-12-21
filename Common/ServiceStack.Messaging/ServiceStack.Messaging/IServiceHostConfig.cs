using System;
using System.Text;

namespace ServiceStack.Messaging
{
    public interface IServiceHostConfig
    {
        /// <summary>
        /// Gets the URI.
        /// </summary>
        /// <value>The URI.</value>
        string Uri
        {
            get;
        }

        /// <summary>
        /// Gets the type of the service.
        /// </summary>
        /// <value>The type of the service.</value>
        Type ServiceType
        {
            get;
        }

        /// <summary>
        /// Gets the dead letter queue.
        /// </summary>
        /// <value>The dead letter queue.</value>
        string DeadLetterQueue
        {
            get;
        }

        /// <summary>
        /// Gets the failover settings.
        /// </summary>
        /// <value>The failover settings.</value>
        FailoverSettings FailoverSettings
        {
            get;
        }

        /// <summary>
        /// Gets the max redelivery count.
        /// </summary>
        /// <value>The max redelivery count.</value>
        int MaxRedeliveryCount
        {
            get;
        }
    }
}
