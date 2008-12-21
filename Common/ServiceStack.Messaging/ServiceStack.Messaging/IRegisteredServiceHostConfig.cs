using System;
using System.Text;

namespace ServiceStack.Messaging
{
    public interface IRegisteredServiceHostConfig : IServiceHostConfig
    {
        /// <summary>
        /// Gets the durable subscriber id.
        /// </summary>
        /// <value>The durable subscriber id.</value>
        string DurableSubscriberId
        { 
            get;
        }
    }
}
