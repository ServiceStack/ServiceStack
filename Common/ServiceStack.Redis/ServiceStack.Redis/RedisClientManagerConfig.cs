namespace ServiceStack.Redis
{
	public class RedisClientManagerConfig
	{
		public RedisClientManagerConfig()
		{
			AutoStart = true; //Simplifies the most common use-case - registering in an IOC
		}

		public int? DefaultDb { get; set; }
		public int MaxReadPoolSize { get; set; }
		public int MaxWritePoolSize { get; set; }
		public bool AutoStart { get; set; }
	}
}