namespace ServiceStack.Redis
{
    public interface IHandleClientDispose
    {
        void DisposeClient(RedisNativeClient client);
    }
}