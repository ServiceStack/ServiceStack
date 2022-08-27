using System.Collections.Generic;
using ServiceStack.Redis;

/// <summary>
/// Resolver strategy for resolving hosts and creating clients
/// </summary>
public interface IRedisResolver
{
    /// <summary>
    /// Master Redis Server Endpoint Info 
    /// </summary>
    IRedisEndpoint PrimaryEndpoint { get; }
    int ReadWriteHostsCount { get; }
    int ReadOnlyHostsCount { get; }

    IRedisClient CreateClient(string host);

    void ResetMasters(IEnumerable<string> hosts);
    void ResetSlaves(IEnumerable<string> hosts);
}

public interface IHasRedisResolver
{
    IRedisResolver RedisResolver { get; set; }
}
