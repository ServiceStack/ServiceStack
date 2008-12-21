using System;
using System.Text;

namespace ServiceStack.Messaging
{
    public interface IMessage
    {
        /// <summary>
        /// Gets the correlation id.
        /// </summary>
        /// <value>The correlation id.</value>
        string CorrelationId { get; set; }

        /// <summary>
        /// Gets the expiration.
        /// </summary>
        /// <value>The expiration.</value>
        TimeSpan Expiration { get; set; }

        /// <summary>
        /// Gets the Destination of the message.
        /// </summary>
        /// <value>To.</value>
        IDestination To { get; set; }

        /// <summary>
        /// Gets the reply to.
        /// </summary>
        /// <value>The reply to.</value>
        IDestination ReplyTo { get; set; }

        /// <summary>
        /// Gets the session id.
        /// </summary>
        /// <value>The session id.</value>
        string SessionId { get; set; }

        /// <summary>
        /// Gets the time stamp.
        /// </summary>
        /// <value>The time stamp.</value>
        DateTime TimeStamp { get; }

        /// <summary>
        /// Gets a value indicating whether to persist this <see cref="IMessage"/> contents.
        /// </summary>
        /// <value><c>true</c> to persist; otherwise, <c>false</c>.</value>
        bool Persist { get; set; }
    }
}
