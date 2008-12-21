using System;
using System.Collections.Generic;
using System.Text;

namespace ServiceStack.Messaging.Tests.Services.Basic
{
    public interface IListenerService : IDisposable
    {
        void Start();
    }
}
