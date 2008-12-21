using System;
using System.Collections.Generic;
using System.Text;
using ActiveMQ.Commands;
using ServiceStack.Messaging.ActiveMq.Support.Utils;

namespace ServiceStack.Messaging.Tests.Objects.Mock
{
    public class MockNmsConsumer : NMS.IMessageConsumer
    {
        private const string NMSX_DELIVERY_COUNT = "NMSXDeliveryCount";

        private List<NMS.ITextMessage> textMessages;
        private int receivedNo;
        private int disposedNo;
        private MockNmsSession session;
        private readonly NMS.IDestination destination;

        public MockNmsConsumer(MockNmsSession session, NMS.IDestination destination)
        {
            this.session = session;
            this.destination = destination;
            textMessages = new List<NMS.ITextMessage>();
            receivedNo = 0;
            disposedNo = 0;
        }

        public int ReceivedNo
        {
            get { return receivedNo; }
        }

        public int DisposedNo
        {
            get { return disposedNo; }
        }

        public MockNmsSession Session
        {
            get { return session; }
        }

        public List<NMS.ITextMessage> TextMessages
        {
            get { return textMessages; }
        }

        public void Deliver(NMS.ITextMessage message)
        {
            textMessages.Add(message);
            session.LastMessageSent = new KeyValuePair<NMS.IDestination, NMS.ITextMessage>(destination, message);
            if (Listener != null)
            {
                UpdateNmsxDeliveryCount(message);
                Listener(message);
            }
        }

        public void Deliver(IEnumerable<NMS.ITextMessage> messages)
        {
            foreach (NMS.ITextMessage message in messages)
            {
                Deliver(message);
            }
        }

        private static void UpdateNmsxDeliveryCount(NMS.IMessage message)
        {
            ActiveMQMessage amqMessage = message as ActiveMQMessage;
            if (amqMessage != null)
            {
                amqMessage.RedeliveryCounter++;
            }
            int oldVal = ReflectionUtils.GetPropertyValue<int>(message, NMSX_DELIVERY_COUNT);
            ReflectionUtils.SetPropertyValue<int>(message, NMSX_DELIVERY_COUNT, oldVal + 1);
        }

        public NMS.IDestination Destination
        {
            get { return destination; }
        }

        #region IMessageConsumer Members

        public void Close()
        {
            Dispose();
        }

        public event NMS.MessageListener Listener;

        public NMS.IMessage Receive(TimeSpan timeout)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public NMS.IMessage Receive()
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public NMS.IMessage ReceiveNoWait()
        {
            throw new Exception("The method or operation is not implemented.");
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
