using System.Configuration;
using Enyim.Caching;
using ServiceStack.CacheAccess;
using ServiceStack.CacheAccess.Providers;
using ServiceStack.Logging;
using @ServiceNamespace@.Tests.ServiceInterface.Version100;

namespace @ServiceNamespace@.Tests.Support
{
	/// <summary>
	/// 
	/// </summary>
	public class TestParameters : ITestParameters
	{
		private readonly ILogFactory logFactory = LogManager.LogFactory;

		public string LocalConnectionString
		{
			get { return ConfigurationManager.AppSettings["LocalConnectionString"]; }
		}

		public string UnitTestConnectionString
		{
			get { return ConfigurationManager.AppSettings["TestConnectionString"]; }
		}

		public string DatabaseName
		{
			get { return ConfigurationManager.AppSettings["DatabaseName"]; }
		}

		public string CreateSchemaScript
		{
			get { return ConfigurationManager.AppSettings["CreateSchemaScript"]; }
		}

		public string MappingAssemblyName
		{
			get { return "@ServiceNamespace@.DataAccess"; }
		}

		public ILogFactory LogFactory
		{
			get { return logFactory; }
		}

		/// <summary>
		/// Gets the cache. Configures iteslf from App.config
		/// </summary>
		/// <value>The cache.</value>
		public ICacheClient Cache
		{
			get { return new MemcachedClientCache(new MemcachedClient()); }
		}
	}
}