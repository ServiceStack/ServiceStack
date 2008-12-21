using System;
using System.Collections.Generic;
using System.Text;
using ServiceStack.Messaging.Tests.Services.Basic;

namespace ServiceStack.Messaging.Tests.Services.Messaging
{
    public class ReverseTextService : IService
    {
        #region IService Members

        public string Execute(IServiceHost serviceHost, ITextMessage message)
        {
            return SimpleService.Reverse(message.Text);
        }

        #endregion
    }
}
