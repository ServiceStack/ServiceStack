using System;

namespace ServiceStack.Messaging.Tests.Objects.Mock
{
    public class MockSentInfo
    {
        NMS.IDestination destination;
        NMS.IMessage message; 
        bool persistent; 
        byte priority;
        private TimeSpan timeToLive;

        public MockSentInfo(NMS.IDestination destination, NMS.IMessage message, bool persistent, byte priority, TimeSpan timeToLive)
        {
            this.destination = destination;
            this.message = message;
            this.persistent = persistent;
            this.priority = priority;
            this.timeToLive = timeToLive;
        }

        public NMS.IDestination Destination
        {
            get { return destination; }
            set { destination = value; }
        }

        public NMS.IMessage Message
        {
            get { return message; }
            set { message = value; }
        }

        public bool Persistent
        {
            get { return persistent; }
            set { persistent = value; }
        }

        public byte Priority
        {
            get { return priority; }
            set { priority = value; }
        }

        public TimeSpan TimeToLive
        {
            get { return timeToLive; }
            set { timeToLive = value; }
        }

        public NMS.ITextMessage TextMessage
        {
            get { return message as NMS.ITextMessage; }
        }
    }
}
