using System.Collections.Generic;

namespace ServiceStack.Redis
{
	public class RedisClientLists
	{
		private readonly RedisClient client;

		public RedisClientLists(RedisClient client)
		{
			this.client = client;
		}

		public IList<string> this[string listId]
		{
			get
			{
				return new RedisClientList(client, listId);
			}
		}
	}
}