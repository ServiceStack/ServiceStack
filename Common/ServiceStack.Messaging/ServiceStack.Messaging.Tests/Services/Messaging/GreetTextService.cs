using System;
using System.Collections.Generic;
using System.Text;
using ServiceStack.Messaging.Tests.Services.Basic;

namespace ServiceStack.Messaging.Tests.Services.Messaging
{
    public class GreetTextService : IService
    {
        public string Execute(IServiceHost serviceHost, ITextMessage message)
        {
            return SimpleService.Greet(message.Text);
        }
    }
}
