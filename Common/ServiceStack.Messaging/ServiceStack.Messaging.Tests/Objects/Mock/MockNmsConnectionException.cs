using System;
using System.Collections.Generic;
using System.Text;
using NMS;

namespace ServiceStack.Messaging.Tests.Objects.Mock
{
    public class MockNmsConnectionException : NMSException
    {
        public MockNmsConnectionException(string message, Exception innerException) : base(message, innerException)
        {
        }

        public MockNmsConnectionException(string message) : base(message)
        {
        }
    }
}
