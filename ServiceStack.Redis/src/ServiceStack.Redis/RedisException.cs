using System;

namespace ServiceStack.Redis
{
    /// <summary>
    /// Redis-specific exception. Thrown if unable to connect to Redis server due to socket exception, for example.
    /// </summary>
	public class RedisException
		: Exception
	{
		public RedisException(string message)
			: base(message)
		{
		}
        public RedisException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
	}
}