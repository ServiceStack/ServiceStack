namespace ServiceStack.Redis
{
    public static partial class RedisClientExtensions
    {
        public static string GetHostString(this IRedisClient redis)
        {
            return "{0}:{1}".Fmt(redis.Host, redis.Port);
        }

        public static string GetHostString(this RedisEndpoint config)
        {
            return "{0}:{1}".Fmt(config.Host, config.Port);
        }
    }
}