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
			DefaultRedirectPath = appConfig.Get("DefaultRedirectPath", "Public/Metadata");
		}

		public string RedisHostAddress { get; set; }
		public int RedisDb { get; set; }
		public string DefaultRedirectPath { get; set; }
	}
}