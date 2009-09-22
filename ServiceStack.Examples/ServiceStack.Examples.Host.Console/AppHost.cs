using System;
using ServiceStack.CacheAccess;
using ServiceStack.CacheAccess.Providers;
using ServiceStack.Common.Extensions;
using ServiceStack.Common.Utils;
using ServiceStack.Configuration;
using ServiceStack.DataAccess;
using ServiceStack.DataAccess.Db4oProvider;
using ServiceStack.Examples.ServiceInterface;
using ServiceStack.Examples.ServiceInterface.Types;
using ServiceStack.Logging;
using ServiceStack.Logging.Support.Logging;
using ServiceStack.LogicFacade;
using ServiceStack.Service;
using ServiceStack.ServiceInterface;
using ServiceStack.WebHost.Endpoints;

namespace ServiceStack.Examples.Host.Console
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

			//Set up the Application singleton service providers. Overrideable at runtime via '<objects/>' in web.config 
			LogManager.LogFactory = factory.ResolveOptional<ILogFactory>("LogFactory", new DebugLogFactory());
			var config = factory.ResolveOptional<IResourceManager>("ResourceManager", new ConfigurationResourceManager());
			var cacheClient = factory.ResolveOptional<ICacheClient>("CacheProvider", new MemoryCacheClient()); // for Memcacehed use: 'new MemcachedClientCache()'

			//Example of dynamically registering an external service
			var dbPath = config.GetString("Db4oConnectionString").MapAbsolutePath();
			factory.Register<IPersistenceProviderManager>(new Db4OFileProviderManager(dbPath));


			//Set your Applications Singleton Context. Contains providers that are available to all your services via 'ApplicationContext.Instance'
			ApplicationContext.SetInstanceContext(new BasicApplicationContext(factory, cacheClient, config));

			//Customize ServiceStack's behaviour 
			base.SetConfig(new EndpointHostConfig {

				//The Name that will appear on the Metadata pages
				ServiceName = config.GetString("ServiceName"),

				//Tell ServiceStack where to look for your services
				ServiceController = new ServiceController(
					new PortResolver(new FactoryProviderHandlerFactory(factory),
						typeof(GetFactorialHandler).Assembly)),
			});


			var listeningOn = config.GetString("ListenBaseUrl");
			this.Start(listeningOn);

			//How to use loging in your services (essentially the same as Log4Net, but without the dependancy)
			var log = LogManager.GetLogger(GetType());

			log.InfoFormat("AppHost Created at {0}, listening on {1}, saving to db at {2}",
				DateTime.Now, listeningOn, dbPath);

			if (config.Get("PerformTestsOnInit", false))
			{
				log.Debug("Performing database tests...");
				DatabaseTest();
			}
		}

		private void DatabaseTest()
		{
			using (var db4OManager = new Db4OFileProviderManager("test.db4o"))
			{
				var storeHandler = new StoreNewUserHandler(db4OManager);
				var operationContext = CreateOperationContext(new StoreNewUser {
					Email = "new@email",
					Password = "password",
					UserName = "new UserName"
				}, EndpointAttributes.None);

				storeHandler.Execute(operationContext);

				var getAllHandler = new GetAllUsersHandler(db4OManager);
				var response = (GetAllUsersResponse)getAllHandler.Execute(
					CreateOperationContext(new GetAllUsers(), EndpointAttributes.None));

				var user = response.Users[0];

				System.Console.WriteLine("Stored and retrieved user: {0}, {1}, {2}",
					user.Id, user.UserName, user.Email);
			}
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
			var requestContext = new RequestContext(requestDto, endpointAttributes, new FactoryProvider(FactoryUtils.ObjectFactory));
			return new OperationContext(ApplicationContext.Instance, requestContext);
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