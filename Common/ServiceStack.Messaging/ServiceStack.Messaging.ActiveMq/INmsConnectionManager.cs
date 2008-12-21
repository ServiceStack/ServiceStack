using System;

namespace ServiceStack.Messaging.ActiveMq
{
    public interface INmsConnectionManager : IConnection
    {
        /// <summary>
        /// Gets the factory.
        /// </summary>
        /// <value>The factory.</value>
        IMessagingFactory Factory { get; }

        /// <summary>
        /// Reconnects the specified connection string.
        /// </summary>
        /// <param name="connectionString">The connection string.</param>
        /// <returns></returns>
        NMS.IConnection Reconnect(string connectionString);

        /// <summary>
        /// Gets the NMS connection.
        /// </summary>
        /// <value>The NMS connection.</value>
        NMS.IConnection NmsConnection { get; }
    }
}