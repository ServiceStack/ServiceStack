using System;
using System.Collections.Generic;
using System.Text;
using ActiveMQ.Commands;
using ServiceStack.Messaging.ActiveMq.Support.Utils;
using NMS;

namespace ServiceStack.Messaging.Tests.Objects.Mock
{
    public class MockNmsProducer : NMS.IMessageProducer 
    {
        private MockNmsSession session;
        private NMS.IDestination defaultDestination;
        private List<MockSentInfo> sentMessages;
        private int sendNo;
        private int disposedNo;

        public MockNmsProducer(MockNmsSession session)
        {
            this.session = session;
            sentMessages = new List<MockSentInfo>();
            sendNo = 0;
            disposedNo = 0;
        }

        public NMS.IDestination Destination
        {
            get { return defaultDestination; }
            set { defaultDestination = value; }
        }

        public List<MockSentInfo> SentMessages
        {
            get { return sentMessages; }
        }

        public int SendNo
        {
            get { return sendNo; }
        }

        public int DisposedNo
        {
            get { return disposedNo; }
        }

        #region IMessageProducer Members

        public bool DisableMessageID
        {
            get
            {
                throw new Exception("The method or operation is not implemented.");
            }
            set
            {
                throw new Exception("The method or operation is not implemented.");
            }
        }

        public bool DisableMessageTimestamp
        {
            get
            {
                throw new Exception("The method or operation is not implemented.");
            }
            set
            {
                throw new Exception("The method or operation is not implemented.");
            }
        }

        public bool Persistent
        {
            get
            {
                throw new Exception("The method or operation is not implemented.");
            }
            set
            {
                throw new Exception("The method or operation is not implemented.");
            }
        }

        public byte Priority
        {
            get
            {
                throw new Exception("The method or operation is not implemented.");
            }
            set
            {
                throw new Exception("The method or operation is not implemented.");
            }
        }

        public virtual void Send(NMS.IDestination destination, NMS.IMessage message, bool persistent, byte priority, TimeSpan timeToLive)
        {
            SentMessages.Add(new MockSentInfo(destination, message, persistent, priority, timeToLive));
            session.SendMessage(destination, (NMS.ITextMessage)message);
            sendNo++;
        }

        public virtual void Send(NMS.IDestination destination, NMS.IMessage message)
        {
            Send(destination, message, false, default(byte), default(TimeSpan));
        }

        public virtual void Send(NMS.IMessage message, bool persistent, byte priority, TimeSpan timeToLive)
        {
            Send(null, message, persistent, priority, timeToLive);
        }

        public virtual void Send(NMS.IMessage message)
        {
            Send(defaultDestination, message, false, default(byte), default(TimeSpan));
        }

        public TimeSpan TimeToLive
        {
            get
            {
                throw new Exception("The method or operation is not implemented.");
            }
            set
            {
                throw new Exception("The method or operation is not implemented.");
            }
        }

        #endregion

        #region IDisposable Members

        public void Dispose()
        {
            disposedNo++;
        }

        #endregion
    }
}
