using System;
namespace ServiceStack.Redis
{
    public interface IRedisSentinel : IDisposable
    {
        IRedisClientsManager Start();
    }
}
