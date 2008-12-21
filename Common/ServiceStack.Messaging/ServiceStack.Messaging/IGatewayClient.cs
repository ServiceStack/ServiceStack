using System;
using System.Text;

namespace ServiceStack.Messaging
{
    /// <summary>
    /// An Messaging client used to send messages
    /// </summary>
    public interface IGatewayClient : IDisposable
    {
        /// <summary>
        /// Gets or sets the destination.
        /// </summary>
        /// <value>The destination.</value>
        IDestination Destination
        {
            get;
            set;
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
        /// Gets the connection.
        /// </summary>
        /// <value>The connection.</value>
        IConnection Connection
        {
            get;
        }
    }
}
