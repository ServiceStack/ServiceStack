using System;
using System.Collections.Generic;
using System.Text;

namespace ServiceStack.Messaging.Tests.Services.Messaging
{
    public class TestService : IService
    {
        public string Execute(IServiceHost serviceHost, ITextMessage message)
        {
            return null;
        }
    }
}
