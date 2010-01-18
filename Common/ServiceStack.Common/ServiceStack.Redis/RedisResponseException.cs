using System;

namespace ServiceStack.Redis
{
	public class RedisResponseException 
		: Exception
	{
		public RedisResponseException(string code)
			: base("Response error")
		{
			Code = code;
		}

		public string Code { get; private set; }
	}
}