using System;
using System.Collections.Generic;
using System.Text;
using NMS;

namespace ServiceStack.Messaging.Tests.Objects.Mock
{
    public class MockNmsConnectionWithSessionFailure : MockNmsConnection
    {
        public MockNmsConnectionWithSessionFailure(MockMessagingFactory connectionFactory)
            : base(connectionFactory)
        {
        }

        public override NMS.ISession CreateSession(NMS.AcknowledgementMode mode)
        {
            throw new NMSConnectionException("Create Session Failed");
        }

    }
}
