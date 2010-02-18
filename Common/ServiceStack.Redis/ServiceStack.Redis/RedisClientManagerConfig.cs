namespace ServiceStack.Redis
{
	public class RedisClientManagerConfig
	{
		public int MaxReadPoolSize { get; set; }
		public int MaxWritePoolSize { get; set; }
		public bool AutoStart { get; set; }
	}
}