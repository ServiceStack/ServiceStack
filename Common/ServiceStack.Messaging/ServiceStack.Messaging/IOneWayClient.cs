using System;
using System.Text;

namespace ServiceStack.Messaging
{
    /// <summary>
    /// An Messaging client capable of sending a message to a queue or topic
    /// </summary>
    public interface IOneWayClient : IGatewayClient
    {
        /// <summary>
        /// Sends a message to the queue or topic.
        /// </summary>
        /// <param name="message">The message.</param>
        void SendOneWay(ITextMessage message);
    }
}
