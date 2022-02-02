using System;
using System.Collections.Generic;

namespace ServiceStack.Redis
{
    public interface IRedisFailover
    {
        List<Action<IRedisClientsManager>> OnFailover { get; } 

        void FailoverTo(params string[] readWriteHosts);

        void FailoverTo(IEnumerable<string> readWriteHosts, IEnumerable<string> readOnlyHosts);
    }
}