using System;

namespace ServiceStack.Messaging
{
    /// <summary>
    /// Holds the Message contents and Message Metadata
    /// </summary>
    public interface ITextMessage : IMessage
    {
        /// <summary>
        /// Gets the message contents.
        /// </summary>
        /// <value>The text.</value>
        string Text { get; }
    }
}
