using System.Net;

namespace ServiceStack.Redis
{
	/// <summary>
	/// Provide the factory implementation for creating a RedisCacheClient that 
	/// can be mocked and used by different 'Redis Client Managers' 
	/// </summary>
	public class RedisCacheClientFactory : IRedisClientFactory
	{
		public static RedisCacheClientFactory Instance = new RedisCacheClientFactory();

		public RedisClient CreateRedisClient(string host, int port)
		{
			return new RedisCacheClient(host, port);
		}

		public RedisCacheClient CreateRedisCacheClient(string host, int port)
		{
			return new RedisCacheClient(host, port);
		}
	}
}