using System;
using System.Reflection;
using Funq;
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
using ServiceStack.ServiceHost;
using ServiceStack.ServiceInterface;
using ServiceStack.WebHost.Endpoints;

namespace ServiceStack.Examples.Host.Console
{
	/// <summary>
	/// An example of a AppHost to have your services running inside a webserver.
	/// </summary>
	public class AppHost : XmlSyncReplyHttpListener
	{
		private static ILog log;

		public AppHost(string serviceName, params Assembly[] assembliesWithServices)
			: base(serviceName, assembliesWithServices)
		{
			LogManager.LogFactory = new DebugLogFactory();
			log = LogManager.GetLogger(typeof(AppHost));
		}

		public override void Configure(Container container)
		{
			container.Register<IResourceManager>(new ConfigurationResourceManager());

			var config = container.Resolve<IResourceManager>();

			var dbPath = config.GetString("Db4oConnectionString").MapAbsolutePath();
			container.Register<IPersistenceProviderManager>(new Db4OFileProviderManager(dbPath));

			var listeningOn = config.GetString("ListenBaseUrl");
			this.Start(listeningOn);

			log.InfoFormat("AppHost Created at {0}, listening on {1}, saving to db at {2}",
				DateTime.Now, listeningOn, dbPath);

			if (config.Get("PerformTestsOnInit", false))
			{
				log.Debug("Performing database tests...");
				DatabaseTest();
			}
		}

		private static void DatabaseTest()
		{
			using (var db4OManager = new Db4OFileProviderManager("test.db4o"))
			{
				var storeRequest = new StoreNewUser {
					Email = "new@email",
					Password = "password",
					UserName = "new UserName"
				};

				var storeHandler = new StoreNewUserHandler(db4OManager);
				storeHandler.Execute(storeRequest);

				var getAllHandler = new GetAllUsersHandler(db4OManager);
				var response = (GetAllUsersResponse)getAllHandler.Execute(new GetAllUsers());

				var user = response.Users[0];

				System.Console.WriteLine("Stored and retrieved user: {0}, {1}, {2}",
					user.Id, user.UserName, user.Email);
			}
		}

	}
}