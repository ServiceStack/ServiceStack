using System;
using NHibernate;
using ServiceStack.Logging;

namespace ServiceStack.DataAccess.NHibernateProvider
{
	public class NHibernateProviderManager : IPersistenceProviderManager
	{
		private readonly ILog log = LogManager.GetLogger(typeof(NHibernateProviderManager));
		public string ConnectionString { get; private set; }
		public ISessionFactory SessionFactory { get; private set; }

		internal const string ConnectionStringKey = "connection.connection_string";

		public NHibernateProviderManager(NHibernate.Cfg.Configuration configuration)
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

		~NHibernateProviderManager()
		{
			Dispose(false);
		}

		public void Dispose()
		{
			Dispose(true);
		}

		public void Dispose(bool disposing)
		{
			if (disposing)
				GC.SuppressFinalize(this);

			try
			{
				this.SessionFactory.Dispose();
			}
			catch (Exception ex)
			{
				log.Error("Error disposing of Db4o provider", ex);
			}
		}
	}
}