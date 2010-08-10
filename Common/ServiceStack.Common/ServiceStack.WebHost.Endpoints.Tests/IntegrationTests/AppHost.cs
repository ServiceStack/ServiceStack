using Funq;
using ServiceStack.Common.Utils;
using ServiceStack.Configuration;
using ServiceStack.Logging;
using ServiceStack.Logging.Support.Logging;
using ServiceStack.OrmLite;
using ServiceStack.OrmLite.Sqlite;

namespace ServiceStack.WebHost.Endpoints.Tests.IntegrationTests
{
	/// <summary>
	/// An example of a AppHost to have your services running inside a webserver.
	/// </summary>
	public class AppHost
		: AppHostHttpListenerBase
	{
		private static ILog log;

		public AppHost()
			: base("ServiceStack Examples", typeof(MovieRestService).Assembly)
		{
			LogManager.LogFactory = new DebugLogFactory();
			log = LogManager.GetLogger(typeof(AppHost));
		}

		public override void Configure(Container container)
		{
			container.Register<IResourceManager>(new ConfigurationResourceManager());

			var appSettings = container.Resolve<IResourceManager>();

			container.Register<IDbConnectionFactory>(c =>
				 new OrmLiteConnectionFactory(
					":memory:",
					false,
             		SqliteOrmLiteDialectProvider.Instance));

			log.Debug("Performing database tests...");
			DatabaseTest(container.Resolve<IDbConnectionFactory>());
		}

		private static void DatabaseTest(IDbConnectionFactory connectionFactory)
		{
			ConfigureDatabase.Init(connectionFactory);
		}

	}
}