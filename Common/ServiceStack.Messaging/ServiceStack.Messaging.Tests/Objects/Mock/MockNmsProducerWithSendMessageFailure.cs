using System;
using System.Collections.Generic;
using System.Text;
using ActiveMQ.Commands;
using ServiceStack.Messaging.ActiveMq.Support.Utils;

namespace ServiceStack.Messaging.Tests.Objects.Mock
{
    public class MockNmsProducerWithSendMessageFailure : MockNmsProducer
    {
        public MockNmsProducerWithSendMessageFailure(MockNmsSession session) : base(session)
        {
        }

        public override void Send(NMS.IDestination destination, NMS.IMessage message, bool persistent, byte priority, TimeSpan timeToLive)
        {
            SentMessages.Add(new MockSentInfo(destination, message, persistent, priority, timeToLive));
            throw new MockNmsSendMessageException("Error while sending message");
        }

    }
}
