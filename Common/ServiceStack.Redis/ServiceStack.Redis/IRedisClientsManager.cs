using System;

namespace ServiceStack.Redis
{
	public interface IRedisClientsManager : IDisposable 
	{
		IRedisClient GetClient();
		IRedisClient GetReadOnlyClient();
	}
}