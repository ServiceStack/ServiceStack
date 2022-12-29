using ServiceStack.Redis.Pipeline;

namespace ServiceStack.Redis.Generic
{
    /// <summary>
    /// Interface to redis typed pipeline
    /// </summary>
    public interface IRedisTypedPipeline<T> : IRedisPipelineShared, IRedisTypedQueueableOperation<T>
    {
    }
}
