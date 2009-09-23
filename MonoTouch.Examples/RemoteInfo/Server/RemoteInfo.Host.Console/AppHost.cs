using System;
using RemoteInfo.ServiceInterface;
using ServiceStack.CacheAccess;
using ServiceStack.CacheAccess.Providers;
using ServiceStack.Common.Extensions;
using ServiceStack.Configuration;
using ServiceStack.Logging;
using ServiceStack.Logging.Support.Logging;
using ServiceStack.LogicFacade;
using ServiceStack.Service;
using ServiceStack.ServiceInterface;
using ServiceStack.WebHost.Endpoints;

namespace RemoteInfo.Host.Console
{
	/// <summary>
	/// An example of a AppHost to have your services running inside a webserver.
	/// </summary>
	public class AppHost : XmlSyncReplyHttpListener
	{
		public static AppHost Instance;

		/// <summary>
		/// Configure this applicaiton instance.  
		/// Called by main() in Program.cs
		/// </summary>
		public static void Init()
		{
			if (Instance == null)
			{
				Instance = new AppHost();
			}
		}

		private AppHost()
		{
			//The factory is responsible for creating instances of providers defined in <objects/> in the web.config 
			var factory = new FactoryProvider(FactoryUtils.ObjectFactory);

			//Set up the Application providers. Overrideable at runtime via '<objects/>' in web.config 
			LogManager.LogFactory = factory.ResolveOptional<ILogFactory>("LogFactory", new DebugLogFactory());				// prints to Debug output
			var config = factory.ResolveOptional<IResourceManager>("ResourceManager", new ConfigurationResourceManager());  // uses <appSettings />
			var cacheClient = factory.ResolveOptional<ICacheClient>("CacheProvider", new MemoryCacheClient());              // uses In-Memory Cache

			//Declare any dependencies you want injected in handlers
			factory.Register(new RemoteInfoConfig(config));

			//Set your Applications Singleton Context. Contains providers that are available to all your services via 'ApplicationContext.Instance'
			ApplicationContext.SetInstanceContext(new BasicApplicationContext(factory, cacheClient, config));

			//Customize ServiceStack's behaviour 
			base.SetConfig(new EndpointHostConfig {

				//The Name that will appear on the Metadata pages
				ServiceName = config.GetString("ServiceName"),

				//Tell ServiceStack where to look for your services
				ServiceController = new ServiceController(
					new PortResolver(new FactoryProviderHandlerFactory(factory),
									 typeof(GetDirectoryInfoHandler).Assembly)),
			});


			var listeningOn = config.GetString("ListenBaseUrl");
			this.Start(listeningOn);

			//How to use loging in your services (essentially the same as Log4Net, but without the dependancy)
			var log = LogManager.GetLogger(GetType());
			log.InfoFormat("AppHost Created at {0}, listening on {1}", DateTime.Now, listeningOn);
		}


		/// <summary>
		/// Used by ServiceStack to Create the 'Call or OperationContext' for every request.
		/// You can override this to change whats available to your services.
		/// </summary>
		/// <param name="requestDto">The request dto.</param>
		/// <param name="endpointAttributes">The endpoint attributes.</param>
		/// <returns></returns>
		protected override IOperationContext CreateOperationContext(object requestDto, EndpointAttributes endpointAttributes)
		{
			return new OperationContext(ApplicationContext.Instance, new RequestContext(requestDto, endpointAttributes));
		}


		/// <summary>
		/// Clean up application singleton resources when the application is shutting down
		/// </summary>
		public override void Dispose()
		{
			this.Stop();

			new IDisposable[] { ApplicationContext.Instance.Cache, ApplicationContext.Instance.Factory }.Dispose();

			base.Dispose();
		}

	}
}