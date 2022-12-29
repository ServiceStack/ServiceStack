namespace ServiceStack.Redis.Pipeline
{
    /// <summary>
    /// Interface to redis pipeline
    /// </summary>
    public interface IRedisPipelineAsync : IRedisPipelineSharedAsync, IRedisQueueableOperationAsync
    {
    }
}