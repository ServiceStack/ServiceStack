using System;

namespace ServiceStack.Redis.Pipeline
{
	/// <summary>
	/// Pipeline interface shared by typed and non-typed pipelines
	/// </summary>
	public interface IRedisPipelineShared : IDisposable, IRedisQueueCompletableOperation
	{
		void Flush();
		bool Replay();
	}
}