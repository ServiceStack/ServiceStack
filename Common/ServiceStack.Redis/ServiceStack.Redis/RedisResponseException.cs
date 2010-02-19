using System;

namespace ServiceStack.Redis
{
	public class RedisResponseException 
		: Exception
	{
		public RedisResponseException(string message)
			: base(message)
		{
		}

		public RedisResponseException(string message, string code) : base(message)
		{
			Code = code;
		}

		public string Code { get; private set; }
	}
}