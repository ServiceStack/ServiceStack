using System;
using System.Text;

namespace ServiceStack.Messaging
{
    public interface IService 
    {
        string Execute(IServiceHost serviceHost, ITextMessage message);
    }
}
