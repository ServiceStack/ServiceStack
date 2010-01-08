using System.Collections.Generic;

namespace ServiceStack.Redis
{
	public class RedisClientSets
	{
		private readonly RedisClient client;

		public RedisClientSets(RedisClient client)
		{
			this.client = client;
		}

		public ICollection<string> this[string setId]
		{
			get
			{
				return new RedisClientSet(client, setId);
			}
		}
	}
}