using System;
using System.Collections.Generic;
using System.Text;
using NMS;

namespace ServiceStack.Messaging.Tests.Objects.Mock
{
    public class MockNmsSendMessageException : NMSException
    {
        public MockNmsSendMessageException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        public MockNmsSendMessageException(string message)
            : base(message)
        {
        }
    }
}
