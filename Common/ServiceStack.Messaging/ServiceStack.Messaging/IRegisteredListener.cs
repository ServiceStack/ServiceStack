using System;
using System.Text;

namespace ServiceStack.Messaging
{
    /// <summary>
    /// Durable Topic Subscriber
    /// </summary>
    public interface IRegisteredListener : IGatewayListener
    {
        /// <summary>
        /// Gets or sets the durable subscriber id.
        /// </summary>
        /// <value>The durable subscriber id.</value>
        string SubscriberId
        {
            get;
            set;
        }
    }
}
