using NHibernate;
using NHibernate.Cfg;

namespace ServiceStack.DataAccess.NHibernateProvider
{
	public class NHibernateProviderManager : IPersistenceProviderManager
	{
		public string ConnectionString { get; private set; }
		public ISessionFactory SessionFactory { get; private set; }

		internal const string ConnectionStringKey = "connection.connection_string";

		public NHibernateProviderManager(Configuration configuration)
		{
			// Build the NHibernate session factory for the configuration
			this.SessionFactory = configuration.BuildSessionFactory();

			// Extract the connection string from the NHibernate configuration
			this.ConnectionString = configuration.Properties[ConnectionStringKey];
		}

		public IPersistenceProvider CreateProvider()
		{
			return new NHibernatePersistenceProvider(this.SessionFactory);
		}
	}
}