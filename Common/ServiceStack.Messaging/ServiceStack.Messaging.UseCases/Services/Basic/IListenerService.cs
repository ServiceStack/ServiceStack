using System;
using System.Collections.Generic;
using System.Text;

namespace ServiceStack.Messaging.UseCases.Services.Basic
{
    public interface IListenerService : IDisposable
    {
        void Start();
    }
}