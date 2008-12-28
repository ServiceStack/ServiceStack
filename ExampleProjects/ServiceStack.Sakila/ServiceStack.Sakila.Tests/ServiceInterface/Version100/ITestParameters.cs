using ServiceStack.CacheAccess;
using ServiceStack.Logging;

namespace ServiceStack.Sakila.Tests.ServiceInterface.Version100
{
	public interface ITestParameters
	{
		string LocalConnectionString { get; }

		string UnitTestConnectionString { get; }

		string DatabaseName { get; }

		string CreateSchemaScript { get; }

		string MappingAssemblyName { get; }

		string ServerPrivateKeyXml { get; }

		string StringResourcesFilePath { get; }

		ILogFactory LogFactory { get; }

		ICacheClient Cache { get; }
	}
}