using System;
using System.Text;

namespace ServiceStack.Messaging
{
    public interface IServiceHost
    {
        /// <summary>
        /// Gets the gateway listener.
        /// </summary>
        /// <value>The gateway listener.</value>
        IGatewayListener GatewayListener
        { 
            get;
        }

        /// <summary>
        /// Gets the service host config.
        /// </summary>
        /// <value>The service host config.</value>
        IServiceHostConfig Config
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

        /// <summary>
        /// Creates the instance.
        /// </summary>
        IService CreateInstance();
    }
}
