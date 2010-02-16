using System.Net;

namespace ServiceStack.Redis
{
	public interface IRedisClientFactory
	{
		RedisClient CreateRedisClient(string host, int port);
	}
}