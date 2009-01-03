using @ServiceModelNamespace@;
using ServiceStack.CacheAccess.Providers;
using ServiceStack.Configuration;
using ServiceStack.DataAccess.Db4oProvider;
using ServiceStack.Logging;
using ServiceStack.Logging.Log4Net;
using ServiceStack.LogicFacade;
using @ServiceNamespace@.ServiceInterface;
using ServiceStack.ServiceInterface;
using ServiceStack.WebHost.Endpoints;

namespace @ServiceNamespace@.Host.WebService
{
	public class AppHost : EndpointHostBase
	{
		public static AppHost Instance = new AppHost();

		private AppHost()
		{
			LogManager.LogFactory = new Log4NetFactory(true);

			var factory = new FactoryProvider(FactoryUtils.ObjectFactory, LogManager.LogFactory);
			factory.Register(new Db4oFileProviderManager(Config.ConnectionString));

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

		protected override ICallContext CreateCallContext(object requestDto)
		{
			var requestContext = new RequestContext(requestDto, new FactoryProvider(FactoryUtils.ObjectFactory));
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
		}
	}
}
