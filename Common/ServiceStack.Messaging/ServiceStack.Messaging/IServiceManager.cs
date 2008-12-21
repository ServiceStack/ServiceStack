using System;
using System.Text;

namespace ServiceStack.Messaging
{
    public interface IServiceManager : IDisposable
    {
        void Start();
    }
}
