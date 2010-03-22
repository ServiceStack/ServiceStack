using System;
using System.Collections.Generic;

namespace ServiceStack.Redis
{
	/// <summary>
	/// Experimental support, has not been tested.
	/// </summary>
	public class RedisAtomicCommand 
		: IRedisAtomicCommand
	{
		private readonly List<Action<IRedisClient>> commands = new List<Action<IRedisClient>>();

		private readonly RedisClient redisClient;

		public RedisAtomicCommand(RedisClient redisClient)
		{
			this.redisClient = redisClient;
		}

		public void QueueCommand(Action<IRedisClient> command)
		{
			commands.Add(command);
		}

		public void Dispose()
		{
			redisClient.Multi();
			try
			{
				foreach (var command in commands)
				{
					command(redisClient);
				}
			}
			catch
			{
				redisClient.Discard();
			}
			redisClient.Exec();
		}
	}
}