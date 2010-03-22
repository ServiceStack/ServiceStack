using System;

namespace ServiceStack.Redis
{
	public interface IRedisAtomicCommand 
		: IDisposable
	{
		void QueueCommand(Action<IRedisClient> command);
	}
}