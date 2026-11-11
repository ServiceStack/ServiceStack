using System;
using System.Threading;
using System.Threading.Tasks;

namespace ServiceStack.Redis;

public static partial class RedisClientExtensions
{
    public static string GetHostString(this IRedisClient redis) => redis.Host.IndexOf(':') >= 0 ? $"[{redis.Host}]:{redis.Port}" : $"{redis.Host}:{redis.Port}";

    public static string GetHostString(this RedisEndpoint config) => config.Host.IndexOf(':') >= 0 ? $"[{config.Host}]:{config.Port}" : $"{config.Host}:{config.Port}";

    [Obsolete("Use AppendTo")]
    public static long AppendToValue(this IRedisClient redis, string key, string value) =>
        redis.AppendTo(key, value);
    
    [Obsolete("Use AppendToAsync")]
    public static ValueTask<long> AppendToValueAsync(this IRedisClientAsync redis, string key, string value, CancellationToken token = default) =>
        redis.AppendToAsync(key, value, token);
    
}