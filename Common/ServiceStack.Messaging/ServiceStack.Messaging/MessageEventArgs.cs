using System;
using System.Text;

namespace ServiceStack.Messaging
{
    /// <summary>
    /// Event args provided in the MessageReceived event
    /// </summary>
    public class MessageEventArgs : EventArgs
    {
        private readonly ITextMessage message;

        public MessageEventArgs(ITextMessage message)
        {
            this.message = message;
        }

        /// <summary>
        /// Gets the text message.
        /// </summary>
        /// <value>The text message.</value>
        public ITextMessage Message
        {
            get { return message; }
        }
    }
}
