using Db4objects.Db4o;
using ServiceStack.CacheAccess.Providers;
using ServiceStack.Configuration;
using ServiceStack.DataAccess.Db4oProvider;
using ServiceStack.Logging;
using ServiceStack.Logging.Log4Net;
using ServiceStack.LogicFacade;
using @ServiceNamespace@.ServiceInterface;
using ServiceStack.ServiceInterface;
using ServiceStack.WebHost.Endpoints;
using @ServiceModelNamespace@;

namespace @ServiceNamespace@.Host.WebService
{
	public class AppHost : EndpointHostBase
	{
		public static AppHost Instance = new AppHost();

		private AppHost()
		{
			LogManager.LogFactory = new Log4NetFactory(true);

			var factory = new FactoryProvider(FactoryUtils.ObjectFactory, LogManager.LogFactory);
			var providerManager = new Db4oFileProviderManager(Config.ConnectionString);

			var configDb4o = Db4oFactory.Configure();
			configDb4o.ActivationDepth(5);
			configDb4o.UpdateDepth(5);
			configDb4o.OptimizeNativeQueries(true);

			factory.Register(providerManager); //Keep the manager from disposing providers it created
			factory.Register(providerManager.GetProvider());

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
			return new @DatabaseName@OperationContext(ApplicationContext.Instance, requestContext);
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

			public static string ServerPrivateKey
			{
				get { return ConfigUtils.GetAppSetting("ServerPrivateKey"); }
			}
		}
	}
}