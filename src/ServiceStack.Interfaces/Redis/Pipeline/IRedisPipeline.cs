namespace ServiceStack.Redis.Pipeline
{
	/// <summary>
	/// Interface to redis pipeline
	/// </summary>
	public interface IRedisPipeline : IRedisPipelineShared, IRedisQueueableOperation
	{
	}
}