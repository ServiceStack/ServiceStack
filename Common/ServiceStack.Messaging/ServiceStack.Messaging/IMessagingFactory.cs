using System;
using System.Text;

namespace ServiceStack.Messaging
{
    /// <summary>
    /// Allows connections to be created
    /// </summary>
    public interface IMessagingFactory
    {
        /// <summary>
        /// Creates the connection.
        /// </summary>
        /// <param name="connectionString">The connection string.</param>
        /// <returns></returns>
        IConnection CreateConnection(string connectionString);

        /// <summary>
        /// Creates the connection.
        /// </summary>
        /// <returns></returns>
        IConnection CreateConnection();

        /// <summary>
        /// Creates the service host.
        /// </summary>
        /// <param name="listener">The listener.</param>
        /// <param name="config">The config.</param>
        /// <returns></returns>
        IServiceHost CreateServiceHost(IGatewayListener listener, IServiceHostConfig config);
    }
}
