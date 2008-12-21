using System;

namespace ServiceStack.Messaging.ActiveMq
{
    public interface IReplyAsyncResult : IAsyncResult
    {
        bool Set();
    }
}