using System.Collections.Generic;
using ServiceStack.Logging;
using NHibernate.Cfg;

namespace ServiceStack.DataAccess.NHibernateProvider
{
	public class NHibernateProviderManagerFactory : IPersistenceProviderManagerFactory
	{
		private readonly ILog log;

		public IDictionary<string, string> StaticConfigPropertyTable { get; set; }
		public IList<string> XmlMappingAssemblyNames { get; set; }

		public NHibernateProviderManagerFactory(ILogFactory logFactory)
		{
			this.StaticConfigPropertyTable = new Dictionary<string, string>();
			this.XmlMappingAssemblyNames = new List<string>();
			this.log = logFactory.GetLogger(GetType());
		}

		public IPersistenceProviderManager CreateProviderManager(string connectionString)
		{
			// Create the NHibernate configuration programmatically
			Configuration configuration = this.BuildNHibernateConfiguration(connectionString);

			// Create a provider manager wrapper around a session factory
			return new NHibernateProviderManager(configuration);
		}

		private Configuration BuildNHibernateConfiguration(string connectionString)
		{
			IDictionary<string, string> configPropertyTable = new Dictionary<string, string>(this.StaticConfigPropertyTable);
			configPropertyTable[NHibernateProviderManager.ConnectionStringKey] = connectionString;

			Configuration config = new Configuration().SetProperties(configPropertyTable);

			foreach (var assemblyName in this.XmlMappingAssemblyNames)
			{
				config.AddAssembly(assemblyName);
			}

			return config;
		}
	}
}