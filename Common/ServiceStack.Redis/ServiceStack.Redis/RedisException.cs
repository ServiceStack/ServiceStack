using System;

namespace ServiceStack.Redis
{
	public class RedisException
		: Exception
	{
		public RedisException(string message)
			: base(message)
		{
		}

		public RedisException(string message, string code)
			: base(message)
		{
		}
	}
}