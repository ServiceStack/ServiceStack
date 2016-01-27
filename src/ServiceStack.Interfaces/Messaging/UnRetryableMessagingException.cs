using System;

namespace ServiceStack.Messaging
{
    /// <summary>
    /// For messaging exceptions that should by-pass the messaging service's configured
    /// retry attempts and store the message straight into the DLQ
    /// </summary>
    public class UnRetryableMessagingException
        : MessagingException
    {
        public UnRetryableMessagingException() { }

        public UnRetryableMessagingException(string message) : base(message) { }

        public UnRetryableMessagingException(string message, Exception innerException) : base(message, innerException) { }
    }
}
