using System;
using ServiceStack.Common.Extensions;
using ServiceStack.Text;

namespace ServiceStack.Redis
{
	public class RedisLock 
		: IDisposable
	{
		private readonly RedisClient redisClient;
		private readonly string key;

		public RedisLock(RedisClient redisClient, string key, TimeSpan? timeOut)
		{
			this.redisClient = redisClient;
			this.key = key;

			ExecExtensions.RetryUntilTrue(
				() => redisClient.SetIfNotExists(key, "lock " + DateTime.UtcNow.ToUnixTime()),
				timeOut
			);
		}

		public void Dispose()
		{
			redisClient.Remove(key);
		}
	}
}