using System.Net;

namespace ServiceStack.Redis
{
	/// <summary>
	/// Provide the default factory implementation for creating a RedisClient that 
	/// can be mocked and used by different 'Redis Client Managers' 
	/// </summary>
	public class RedisClientFactory : IRedisClientFactory
	{
		public static RedisClientFactory Instance = new RedisClientFactory();

		public RedisClient CreateRedisClient(string host, int port)
		{
			return new RedisClient(host, port);
		}
	}
}