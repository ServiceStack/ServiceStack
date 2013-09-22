using ServiceStack.Configuration;

namespace ServiceStack.WebHost.Endpoints.Tests.IntegrationTests
{
	public class ExampleConfig
	{
		/// <summary>
		/// Would've preferred to use [assembly: ContractNamespace] attribute but it is not supported in Mono
		/// </summary>
		public const string DefaultNamespace = "http://schemas.servicestack.net/types";

		public ExampleConfig() { }

		public ExampleConfig(IAppSettings appConfig)
		{
			ConnectionString = appConfig.GetString("ConnectionString");
			DefaultFibonacciLimit = appConfig.Get("DefaultFibonacciLimit", 10);
		}

		public string ConnectionString { get; set; }
		public int DefaultFibonacciLimit { get; set; }

	}
}