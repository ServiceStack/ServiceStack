using Funq;
using ServiceStack.Common.Utils;
using ServiceStack.Configuration;
using ServiceStack.Examples.ServiceInterface;
using ServiceStack.Examples.ServiceInterface.Support;
using ServiceStack.Logging;
using ServiceStack.Logging.Support.Logging;
using ServiceStack.OrmLite;
using ServiceStack.OrmLite.Sqlite;
using ServiceStack.WebHost.Endpoints;

namespace ServiceStack.Examples.Tests.Integration
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

			container.Register(c => new ExampleConfig(c.Resolve<IResourceManager>()));
			var appConfig = container.Resolve<ExampleConfig>();

			container.Register<IDbConnectionFactory>(c =>
				 new OrmLiteConnectionFactory(
					":memory:",
					false,
					SqliteOrmLiteDialectProvider.Instance));

			ConfigureDatabase.Init(container.Resolve<IDbConnectionFactory>());
		}

	}
}