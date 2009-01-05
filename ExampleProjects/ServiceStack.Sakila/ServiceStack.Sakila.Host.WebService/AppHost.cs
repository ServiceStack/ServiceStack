using System.Collections.Generic;
using Sakila.ServiceModel;
using ServiceStack.CacheAccess.Providers;
using ServiceStack.Configuration;
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
			var nhFactory = NHibernateProviderManagerFactory.CreateMySqlFactory(Config.XmlMappingAssemblyNames);
			factory.Register(nhFactory.CreateProviderManager(Config.ConnectionString));

			// Create the AppContext injected with the static service implementations
			ApplicationContext.SetInstanceContext(new ApplicationContext {
				Cache = new MemoryCacheClient(),
				Factory = factory,
				Resources = new ConfigurationResourceManager(),
			});

			SetConfig(new EndpointHostConfig {
				ServiceName = Config.ServiceName,
				OperationsNamespace = Config.OperationNamespace,
				ServiceModelFinder = ServiceModelFinder.Instance,
				ServiceController = new ServiceController(new ServiceResolver()),
			});
		}

		protected override IOperationContext CreateOperationContext(object requestDto)
		{
			// Create a facade around a provider connection
			ISakilaServiceFacade facade = new SakilaServiceFacade(ApplicationContext.Instance);
			var requestContext = new RequestContext(requestDto, new FactoryProvider(FactoryUtils.ObjectFactory, facade));
			return new OperationContext(ApplicationContext.Instance, requestContext);
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
