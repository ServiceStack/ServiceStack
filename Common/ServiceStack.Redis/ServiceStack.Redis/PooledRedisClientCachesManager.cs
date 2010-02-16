using System.Net;

namespace ServiceStack.Redis
{
	public class PooledRedisClientCachesManager 
		: PooledRedisClientsManager
	{
		public override RedisClient CreateRedisClient(IPEndPoint hostEndpoint)
		{
			return new RedisCacheClient(hostEndpoint.Address.ToString(), hostEndpoint.Port);
		}
	}
}