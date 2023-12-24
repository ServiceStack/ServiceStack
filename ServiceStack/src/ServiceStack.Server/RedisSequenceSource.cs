using ServiceStack.Redis;

namespace ServiceStack;

public class RedisSequenceSource(IRedisClientsManager redisManager) : ISequenceSource
{
    public void InitSchema() {}

    public long Increment(string key, long amount = 1)
    {
        using var redis = redisManager.GetClient();
        return redis.IncrementValueBy("seq:" + key, amount);
    }

    public void Reset(string key, long startingAt = 0)
    {
        using var redis = redisManager.GetClient();
        redis.IncrementValueBy("seq:" + key, startingAt);
    }
}