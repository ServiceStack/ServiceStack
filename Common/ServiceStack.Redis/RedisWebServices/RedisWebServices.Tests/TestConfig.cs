using ServiceStack.Configuration;

namespace RedisWebServices.Tests
{
	public class TestConfig
	{
		public TestConfig() {}

		public TestConfig(IResourceManager appConfig)
		{
			RedisHostAddress = appConfig.Get("RedisHostAddress", "localhost:6379");
			RunIntegrationTests = appConfig.Get("RunIntegrationTests", false);
			IntegrationTestsBaseUrl = appConfig.Get("IntegrationTestsBaseUrl", "http://localhost/RedisWebServices.Host/Public/");
		}

		public string RedisHostAddress { get; set; }
		public bool RunIntegrationTests { get; set; }
		public string IntegrationTestsBaseUrl { get; set; }
	}
}