using System.Collections.Generic;
using Sakila.ServiceModel;
using ServiceStack.CacheAccess.Memcached;
using ServiceStack.Configuration;
using ServiceStack.DataAccess;
using ServiceStack.DataAccess.NHibernateProvider;
using ServiceStack.Logging;
using ServiceStack.Logging.Log4Net;
using ServiceStack.LogicFacade;
using ServiceStack.Sakila.Logic;
using ServiceStack.Sakila.Logic.LogicInterface;
using ServiceStack.Sakila.ServiceInterface;
using ServiceStack.ServiceInterface;
using ServiceStack.WebHost.Endpoints;

namespace ServiceStack.Sakila.Host.WebService
{
	public class AppHost : EndpointHostBase
	{
		public static AppHost Instance = new AppHost();

		private AppHost()
		{
			LogManager.LogFactory = new Log4NetFactory(true);

			var factory = new FactoryProvider(FactoryUtils.ObjectFactory, LogManager.LogFactory);
			factory.Register(CreatePersistenceProviderManager());

			// Create the AppContext injected with the static service implementations
			OperationContext.SetInstanceContext(new OperationContext {
				Cache = new MemoryCacheClient(),
				Factory = factory,
				Resources = new ConfigurationResourceManager(),
			});

			SetConfig(new EndpointHostConfig {
				ServiceName = Config.ServiceName,
				OperationsNamespace = Config.OperationNamespace,
				ServiceModelFinder = ModelInfo.Instance,
				ServiceController = new ServiceController(new ServiceResolver()),
			});
		}

		private static IPersistenceProviderManager CreatePersistenceProviderManager()
		{
			var propertyTable = new Dictionary<string, string> {
            	{"connection.provider", "NHibernate.Connection.DriverConnectionProvider"},
            	{"dialect", "NHibernate.Dialect.MySQLDialect"},
            	{"connection.driver_class", "NHibernate.Driver.MySqlDataDriver"},
            };

			var factory = new NHibernateProviderManagerFactory {
				StaticConfigPropertyTable = propertyTable,
				XmlMappingAssemblyNames = Config.XmlMappingAssemblyNames,
			};
			return factory.CreateProviderManager(Config.ConnectionString);
		}

		protected override ICallContext CreateCallContext(object requestDto)
		{
			// Create a facade around a provider connection
			ISakilaServiceFacade facade = new SakilaServiceFacade(OperationContext.Instance);
			var requestContext = new RequestContext(requestDto, new FactoryProvider(FactoryUtils.ObjectFactory, facade));
			return new CallContext(OperationContext.Instance, requestContext);
		}

		//Access application configuration statically
		public static class Config
		{
			public static string ConnectionString
			{
				get { return ConfigUtils.GetAppSetting("ConnectionString"); }
			}

			public static string ServiceName
			{
				get { return ConfigUtils.GetAppSetting("ServiceName"); }
			}

			public static string OperationNamespace
			{
				get { return ConfigUtils.GetAppSetting("OperationNamespace"); }
			}

			public static List<string> XmlMappingAssemblyNames
			{
				get { return ConfigUtils.GetListFromAppSetting("XmlMappingAssemblyNames"); }
			}
		}
	}
}
