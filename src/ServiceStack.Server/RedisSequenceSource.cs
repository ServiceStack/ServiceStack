using ServiceStack.Redis;

namespace ServiceStack
{
    public class RedisSequenceSource : ISequenceSource
    {
        private readonly IRedisClientsManager redisManager;

        public RedisSequenceSource(IRedisClientsManager redisManager)
        {
            this.redisManager = redisManager;
        }

        public void InitSchema() {}

        public long Increment(string key, int amount = 1)
        {
            using (var redis = redisManager.GetClient())
            {
                return redis.IncrementValueBy("seq:" + key, amount);
            }
        }

        public void Reset(string key, int startingAt = 0)
        {
            using (var redis = redisManager.GetClient())
            {
                redis.IncrementValueBy("seq:" + key, startingAt);
            }
        }
    }
}