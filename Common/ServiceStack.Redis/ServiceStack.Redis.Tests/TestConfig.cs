using ServiceStack.Common.Support;
using ServiceStack.Logging;

namespace ServiceStack.Redis.Tests
{
	public static class TestConfig
	{
		static TestConfig()
		{
			LogManager.LogFactory = new InMemoryLogFactory();
		}

		public const bool IgnoreLongTests = true;

		//public const string SingleHost = "localhost";
		//public static readonly string [] MasterHosts = new[] { "localhost" };
		//public static readonly string [] SlaveHosts = new[] { "localhost" };

		public const string SingleHost = "chi-dev-mem1.ddnglobal.local";
		public static readonly string [] MasterHosts = new[] { "chi-dev-mem1.ddnglobal.local" };
		public static readonly string [] SlaveHosts = new[] { "chi-dev-mem1.ddnglobal.local", "chi-dev-mem2.ddnglobal.local" };
	}
}