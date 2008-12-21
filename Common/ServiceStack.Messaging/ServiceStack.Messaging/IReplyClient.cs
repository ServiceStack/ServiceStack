using System;
using System.Text;

namespace ServiceStack.Messaging
{
    /// <summary>
    /// An Messaging client capable of sending Synchronus and 
    /// Asynchrouns messages to a reply queue or topic
    /// </summary>
    public interface IReplyClient : IOneWayClient
    {
        /// <summary>
        /// Send the message aynchronously to a reply topic or queue.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <returns></returns>
        IAsyncResult BeginSend(ITextMessage message);

        /// <summary>
        /// Ends the send.
        /// </summary>
        /// <param name="asyncResult">The async result.</param>
        /// <param name="timeout">The timeout.</param>
        /// <returns></returns>
        ITextMessage EndSend(IAsyncResult asyncResult, TimeSpan timeout);

        /// <summary>
        /// Synchronously sends the specified message and blocks until it receives a response.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="timeout">The timeout.</param>
        /// <returns></returns>
        ITextMessage Send(ITextMessage message, TimeSpan timeout);
    }
}
