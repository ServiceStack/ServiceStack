using ServiceStack.Configuration;

namespace RedisWebServices.ServiceInterface
{
	public class AppConfig
	{
		public AppConfig() {}

		public AppConfig(IResourceManager appConfig)
		{
			RedisHostAddress = appConfig.Get("RedisHostAddress", "localhost:6379");
			RedisDb = appConfig.Get("RedisDb", 0);
			ComplexTypeEncoding = appConfig.Get("ComplexTypeEncoding", "Json");
			UrnSeperator = appConfig.Get("UrnSeperator", ":");
			DefaultTake = appConfig.Get("DefaultTake", 20);
		}

		public string RedisHostAddress { get; set; }
		public int RedisDb { get; set; }
		public string UrnSeperator { get; set; }
		public string ComplexTypeEncoding { get; set; }
		public int DefaultTake { get; set; }
	}
}