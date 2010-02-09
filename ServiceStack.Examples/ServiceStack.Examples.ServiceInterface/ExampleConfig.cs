using ServiceStack.Common.Utils;
using ServiceStack.Configuration;

namespace ServiceStack.Examples.ServiceInterface
{
	public class ExampleConfig
	{
		public ExampleConfig() {}

		public ExampleConfig(IResourceManager appConfig)
		{
			ConnectionString = appConfig.GetString("ConnectionString").MapAbsolutePath();
			DefaultFibonacciLimit = appConfig.Get("DefaultFibonacciLimit", 10);
		}

		public string ConnectionString { get; set; }
		public int DefaultFibonacciLimit { get; set; }
	}
}