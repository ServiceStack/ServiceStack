using System.Collections.Generic;
using ServiceStack.Logging;

namespace ServiceStack.DataAccess.NHibernateProvider
{
	public class NHibernateProviderManagerFactory : IPersistenceProviderManagerFactory
	{
		private readonly ILog log = LogManager.GetLogger(typeof(NHibernateProviderManagerFactory));

		public IDictionary<string, string> StaticConfigPropertyTable { get; set; }
		public IList<string> XmlMappingAssemblyNames { get; set; }

		public NHibernateProviderManagerFactory()
		{
			this.StaticConfigPropertyTable = new Dictionary<string, string>();
			this.XmlMappingAssemblyNames = new List<string>();
		}

		public IPersistenceProviderManager CreateProviderManager(string connectionString)
		{
			// Create the NHibernate configuration programmatically
			var configuration = this.BuildNHibernateConfiguration(connectionString);

			// Create a provider manager wrapper around a session factory
			return new NHibernateProviderManager(configuration);
		}

		private NHibernate.Cfg.Configuration BuildNHibernateConfiguration(string connectionString)
		{
			IDictionary<string, string> configPropertyTable = new Dictionary<string, string>(this.StaticConfigPropertyTable);
			configPropertyTable[NHibernateProviderManager.ConnectionStringKey] = connectionString;

			var config = new NHibernate.Cfg.Configuration().SetProperties(configPropertyTable);

			foreach (var assemblyName in this.XmlMappingAssemblyNames)
			{
				config.AddAssembly(assemblyName);
			}

			return config;
		}
	}
}