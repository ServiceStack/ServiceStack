namespace ServiceStack.Redis;

internal sealed class RedisPoolReservation(int generation, RedisClient replaced = null)
{
    public int Generation { get; } = generation;
    public RedisClient Replaced { get; } = replaced;
}
