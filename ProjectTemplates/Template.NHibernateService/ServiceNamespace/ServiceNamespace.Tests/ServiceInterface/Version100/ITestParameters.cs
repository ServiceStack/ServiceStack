using ServiceStack.CacheAccess;
using ServiceStack.Logging;

namespace @ServiceNamespace@.Tests.ServiceInterface.Version100
{
	public interface ITestParameters
	{
		string LocalConnectionString { get; }

		string UnitTestConnectionString { get; }

		string DatabaseName { get; }

		string CreateSchemaScript { get; }

		string MappingAssemblyName { get; }

		ILogFactory LogFactory { get; }

		ICacheClient Cache { get; }
	}
}