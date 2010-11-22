using Funq;
using ServiceStack.Common.Utils;
using ServiceStack.Configuration;
using ServiceStack.Examples.ServiceInterface;
using ServiceStack.Examples.ServiceInterface.Support;
using ServiceStack.Examples.ServiceModel.Operations;
using ServiceStack.Logging;
using ServiceStack.Logging.Support.Logging;
using ServiceStack.OrmLite;
using ServiceStack.OrmLite.Sqlite;
using ServiceStack.WebHost.Endpoints;

namespace ServiceStack.Examples.Host.Console
{
	/// <summary>
	/// An example of a AppHost to have your services running inside a webserver.
	/// </summary>
	public class AppHost
		: AppHostHttpListenerBase
	{
		private static ILog log;

		public AppHost()
			: base("ServiceStack Examples", typeof(GetFactorialService).Assembly)
		{
			LogManager.LogFactory = new DebugLogFactory();
			log = LogManager.GetLogger(typeof(AppHost));
		}

		public override void Configure(Container container)
		{
			//Signal advanced web browsers what HTTP Methods you accept
			base.SetConfig(new EndpointHostConfig
			{
				GlobalResponseHeaders =
				{
					{ "Access-Control-Allow-Origin", "*" },
					{ "Access-Control-Allow-Methods", "GET, POST, PUT, DELETE, OPTIONS" },
				},
                WsdlServiceNamespace = "http://www.servicestack.net/",
				WsdlServiceTypesNamespace = "http://www.servicestack.net/types",
			});

			container.Register<IResourceManager>(new ConfigurationResourceManager());

			var appSettings = container.Resolve<IResourceManager>();

			container.Register(c => new ExampleConfig(c.Resolve<IResourceManager>()));
			var appConfig = container.Resolve<ExampleConfig>();

			container.Register<IDbConnectionFactory>(c => 
				new OrmLiteConnectionFactory(
					appConfig.ConnectionString.MapAbsolutePath(), 
					SqliteOrmLiteDialectProvider.Instance));

			if (appSettings.Get("PerformTestsOnInit", false))
			{
				log.Debug("Performing database tests...");
				DatabaseTest(container.Resolve<IDbConnectionFactory>());
			}
		}

		private static void DatabaseTest(IDbConnectionFactory connectionFactory)
		{
			ConfigureDatabase.Init(connectionFactory);

			var storeRequest = new StoreNewUser {
				Email = "new@email",
				Password = "password",
				UserName = "new UserName"
			};

			var storeHandler = new StoreNewUserService { ConnectionFactory = connectionFactory };
			storeHandler.Execute(storeRequest);

			var getAllHandler = new GetAllUsersService { ConnectionFactory = connectionFactory };
			var response = (GetAllUsersResponse)getAllHandler.Execute(new GetAllUsers());

			var user = response.Users[0];

			System.Console.WriteLine("Stored and retrieved user: {0}, {1}, {2}",
				user.Id, user.UserName, user.Email);
		}

	}
}