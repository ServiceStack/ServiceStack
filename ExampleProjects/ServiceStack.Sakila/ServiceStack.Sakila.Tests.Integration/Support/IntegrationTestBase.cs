using System.Collections.Generic;
using NUnit.Framework;
using ServiceStack.Configuration;
using ServiceStack.DataAccess;
using ServiceStack.DataAccess.NHibernateProvider;
using ServiceStack.Logging.Log4Net;
using ServiceStack.Sakila.Logic;
using ServiceStack.Sakila.ServiceInterface;
using ServiceStack.ServiceInterface;
using DataModel = ServiceStack.Sakila.DataAccess.DataModel;

namespace ServiceStack.Sakila.Tests.Integration.Support
{
	public class IntegrationTestBase
	{
		public IntegrationTestBase()
		{
			this.ServiceController = new ServiceController(new ServiceResolver());
			this.Config = new AppConfig();
			this.ProviderManager = CreateProviderManager(this.Config.LocalConnectionString, this.Config.MappingAssemblyName);
			this.AppContext = new AppContext { LogFactory = new Log4NetFactory(true), ResourceManager = new ResourceManager() };
			this.Facade = new SakilaServiceFacade(this.AppContext, this.ProviderManager);
		}

		public ushort CustomerId { get { return 1; } }

		private static IPersistenceProviderManager CreateProviderManager(string connectionString, string mappingAssemblyName)
		{
			// Create the Nhibernate configuration properties using the database connection string
			IDictionary<string, string> properties = new Dictionary<string, string>
			{
				{"connection.provider", "NHibernate.Connection.DriverConnectionProvider"},
				{"dialect", "NHibernate.Dialect.MySQLDialect"},
				{"connection.driver_class", "NHibernate.Driver.MySqlDataDriver"},
				{"connection.connection_string", connectionString},
			};

			try
			{
				var cfg = new NHibernate.Cfg.Configuration().SetProperties(properties);

				// Add the NHibernate mapping assembly to the configuration
				cfg.AddAssembly(mappingAssemblyName);

				// Create the provider manager for the database
				var manager = new NHibernateProviderManager(cfg);
				return manager;
			}
			catch (System.Exception ex)
			{
				throw;
			}
		}

		/// <summary>
		/// DataModel user list
		/// </summary>
		protected List<DataModel.Customer> Customers { get; set; }

		///// <summary>
		///// DataModel Customer Id list
		///// </summary>
		protected List<int> CustomerIds
		{
			get { return this.Customers.ConvertAll(x => (int)x.Id); }
		}

		protected AppConfig Config { get; private set; }
		protected AppContext AppContext { get; private set; }
		protected SakilaServiceFacade Facade { get; private set; }
		protected IPersistenceProviderManager ProviderManager { get; private set; }
		protected ServiceController ServiceController { get; private set; }

		[TestFixtureTearDown]
		public void FixtureTearDown()
		{
			if (this.Facade == null) return;

			this.Facade.Dispose();
			this.Facade = null;
		}

		protected object ExecuteService(object requestDto)
		{
			var context = new CallContext(this.AppContext, new RequestContext(requestDto, this.Facade));
			return this.ServiceController.Execute(context);
		}

	}
}