using System;
using System.Collections.Generic;
using System.Configuration;
using System.Web;
using Enyim.Caching;
using Sakila.ServiceModel;
using ServiceStack.CacheAccess.Memcached;
using ServiceStack.Configuration;
using ServiceStack.DataAccess;
using ServiceStack.DataAccess.NHibernateProvider;
using ServiceStack.Logging;
using ServiceStack.Logging.Log4Net;
using ServiceStack.Sakila.Logic;
using ServiceStack.Sakila.Logic.LogicInterface;
using ServiceStack.Sakila.ServiceInterface;
using ServiceStack.Service;
using ServiceStack.ServiceInterface;
using ServiceStack.WebHost.Endpoints;

namespace ServiceStack.Sakila.Host.WebService
{
	public class App : IServiceHost
	{
		public static App Instance = new App();

		private readonly ILog log;
		public IPersistenceProviderManager DefaultProviderManager { get; private set; }
		public ServiceController ServiceController { get; private set; }

		public App()
		{
			LogManager.LogFactory = new Log4NetFactory(true);
			log = LogManager.GetLogger(this.GetType());

			var before = DateTime.Now;
			log.Info("Begin Initializing OperationContext...");

			// Create the AppContext injected with the static service implementations
			OperationContext.SetInstanceContext(new OperationContext {
				LogFactory = LogManager.LogFactory,
				Cache = new MemoryCacheClient(),
				Factory = new FactoryProvider(FactoryUtils.ObjectFactory),
				Resources = new ConfigurationResourceManager(),
			});

			OperationContext.Instance.Factory.Register(CreateNHibernateProviderManager(Config.ConnectionString));

			// Create the service controller
			this.ServiceController = new ServiceController(new ServiceResolver());

			EndpointHost.Config = new EndpointHostConfig {
				ServiceHost = this,
				ModelInfo = ModelInfo.Instance,
				OperationsNamespace = "Sakila.ServiceModel.Version100.Operations.SakilaService",
				ServiceName = "Sakila Service",
			};

			var elapsed = DateTime.Now - before;
			log.InfoFormat("Initializing OperationContext took {0}ms", elapsed.TotalMilliseconds);
		}

		public object ExecuteService(object requestDto)
		{
			using (CallContext context = CreateCallContext(requestDto))
			{
				return this.ServiceController.Execute(context);
			}
		}

		public string ExecuteXmlService(string xml)
		{
			// Create a xml request DTO which the service controller will parse and reassign the call
			// context request DTO to a object expected by the relevant port
			var requestDto = new XmlRequestDto(xml, ModelInfo.Instance);

			using (CallContext context = CreateCallContext(requestDto))
			{
				return this.ServiceController.ExecuteXml(context);
			}
		}

		private CallContext CreateCallContext(object requestDto)
		{
			// Create a facade around a provider connection
			ISakilaServiceFacade facade = new SakilaServiceFacade(OperationContext.Instance);

			// Populate the request context
			var requestContext = new RequestContext(requestDto, new FactoryProvider(FactoryUtils.ObjectFactory, facade));

			return new CallContext(OperationContext.Instance, requestContext);
		}

		private static IPersistenceProviderManager CreateNHibernateProviderManager(string connectionString)
		{
			var propertyTable = new Dictionary<string, string>
			                    {
			                    	{"connection.provider", "NHibernate.Connection.DriverConnectionProvider"},
			                    	{"dialect", "NHibernate.Dialect.MySQLDialect"},
			                    	{"connection.driver_class", "NHibernate.Driver.MySqlDataDriver"},
			                    };

			var xmlMappingAssemblyNames = new List<string> { "ServiceStack.Sakila.DataAccess", };

			var factory = new NHibernateProviderManagerFactory(OperationContext.Instance.LogFactory) {
				StaticConfigPropertyTable = propertyTable,
				XmlMappingAssemblyNames = xmlMappingAssemblyNames,
			};
			return factory.CreateProviderManager(connectionString);
		}

		public static class Config
		{
			public static string ConnectionString
			{
				get { return ConfigUtils.GetAppSetting("ConnectionString"); }
			}

			public static string ServerPrivateKey
			{
				get { return ConfigUtils.GetAppSetting("ServerPrivateKey"); }
			}
		}
	}
}
