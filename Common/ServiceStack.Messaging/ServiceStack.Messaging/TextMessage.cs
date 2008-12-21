using System;

namespace ServiceStack.Messaging
{
    public class TextMessage : ITextMessage
    {
        private string text;
        private string correlationId;
        private TimeSpan expiration;
        private IDestination to;
        private IDestination replyTo;
        private string sessionId;
        private DateTime timeStamp;
        private bool persist;

        public TextMessage()
            : this((string)null) { }

        public TextMessage(string text)
        {
            this.text = text;
            correlationId = null;
            expiration = TimeSpan.MaxValue;
            to = null;
            replyTo = null;
            sessionId = null;
            timeStamp = DateTime.UtcNow;
            persist = false;
        }

        public TextMessage(string text, IMessage properties)
        {
            this.text = text;
            CorrelationId = properties.CorrelationId;
            Expiration = properties.Expiration;
            To = properties.To;
            ReplyTo = properties.ReplyTo;
            SessionId = properties.SessionId;
            timeStamp = properties.TimeStamp;
            Persist = properties.Persist;
        }

        public string Text
        {
            get { return text; }
        }

        public string CorrelationId
        {
            get { return correlationId; }
            set { correlationId = value; }
        }

        public TimeSpan Expiration
        {
            get { return expiration; }
            set { expiration = value; }
        }

        public IDestination To
        {
            get { return to; }
            set { to = value; }
        }

        public IDestination ReplyTo
        {
            get { return replyTo; }
            set { replyTo = value; }
        }

        public string SessionId
        {
            get { return sessionId; }
            set { sessionId = value; }
        }

        public DateTime TimeStamp
        {
            get { return timeStamp; }
            set { timeStamp = value; }
        }

        public bool Persist
        {
            get { return persist; }
            set { persist = value; }
        }
    }
}