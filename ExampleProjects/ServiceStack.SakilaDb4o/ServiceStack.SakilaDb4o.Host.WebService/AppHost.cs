using Sakila.ServiceModel;
using ServiceStack.CacheAccess.Providers;
using ServiceStack.Configuration;
using ServiceStack.DataAccess.Db4oProvider;
using ServiceStack.Logging;
using ServiceStack.Logging.Log4Net;
using ServiceStack.LogicFacade;
using ServiceStack.SakilaDb4o.ServiceInterface;
using ServiceStack.ServiceInterface;
using ServiceStack.WebHost.Endpoints;

namespace ServiceStack.SakilaDb4o.Host.WebService
{
	public class AppHost : EndpointHostBase
	{
		public static AppHost Instance = new AppHost();

		private AppHost()
		{
			LogManager.LogFactory = new Log4NetFactory(true);

			var factory = new FactoryProvider(FactoryUtils.ObjectFactory, LogManager.LogFactory);
			factory.Register(new Db4oFileProviderManager(Config.ConnectionString));

			// Create the ApplicationContext injected with the static service implementations
			ApplicationContext.SetInstanceContext(new ApplicationContext {
				Factory = factory,
				Cache = new MemoryCacheClient(),
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
			var requestContext = new RequestContext(requestDto, new FactoryProvider(FactoryUtils.ObjectFactory));
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
		}
	}
}
