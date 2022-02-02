namespace ServiceStack.Redis
{
    public enum RedisServerRole
    {
        Unknown,
        Master,
        Slave,
        Sentinel,
    }
}