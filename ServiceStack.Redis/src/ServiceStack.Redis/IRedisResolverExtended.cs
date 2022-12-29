using System;

namespace ServiceStack.Redis;

public interface IRedisResolverExtended : IRedisResolver
{
    RedisEndpoint[] Masters { get; }
    Func<RedisEndpoint, RedisClient> ClientFactory { get; set; }
    RedisClient CreateMasterClient(int desiredIndex);
    RedisClient CreateSlaveClient(int desiredIndex);

    RedisClient CreateRedisClient(RedisEndpoint config, bool master);

    RedisEndpoint GetReadWriteHost(int desiredIndex);
    RedisEndpoint GetReadOnlyHost(int desiredIndex);
}

public static class RedisResolverExtensions
{
    public static RedisClient CreateMasterClient(this IRedisResolver resolver, int desiredIndex)
    {
        return ((IRedisResolverExtended)resolver).CreateMasterClient(desiredIndex);
    }

    public static RedisClient CreateSlaveClient(this IRedisResolver resolver, int desiredIndex)
    {
        return ((IRedisResolverExtended)resolver).CreateSlaveClient(desiredIndex);
    }

    public static RedisClient CreateRedisClient(this IRedisResolver resolver, RedisEndpoint config, bool master)
    {
        return ((IRedisResolverExtended)resolver).CreateRedisClient(config, master);
    }

    public static RedisEndpoint GetReadWriteHost(this IRedisResolver resolver, int desiredIndex)
    {
        return ((IRedisResolverExtended)resolver).GetReadWriteHost(desiredIndex);
    }

    public static RedisEndpoint GetReadOnlyHost(this IRedisResolver resolver, int desiredIndex)
    {
        return ((IRedisResolverExtended)resolver).GetReadOnlyHost(desiredIndex);
    }
}