using System;
using Funq;
using ServiceStack.CacheAccess;
using ServiceStack.CacheAccess.Providers;
using ServiceStack.Common.Utils;
using ServiceStack.Configuration;
using ServiceStack.DataAccess;
using ServiceStack.Examples.ServiceInterface;
using ServiceStack.Examples.ServiceInterface.Support;
using ServiceStack.Logging;
using ServiceStack.Logging.Support.Logging;
using ServiceStack.OrmLite;
using ServiceStack.OrmLite.Sqlite;
using ServiceStack.Redis;
using ServiceStack.WebHost.Endpoints;

namespace ServiceStack.Examples.Host.Web
{
	/// <summary>
	/// An example of a AppHost to have your services running inside a webserver.
	/// </summary>
	public class AppHost 
		: AppHostBase
	{
		private static ILog log;

		public AppHost()
			: base("ServiceStack Examples", typeof(GetFactorialService).Assembly)
		{
			LogManager.LogFactory = new DebugLogFactory();
			log = LogManager.GetLogger(typeof (AppHost));
		}

		public override void Configure(Container container)
		{
			container.Register<IResourceManager>(new ConfigurationResourceManager());

			container.Register(c => new ExampleConfig(c.Resolve<IResourceManager>()));
			var appConfig = container.Resolve<ExampleConfig>();

			container.Register<IDbConnectionFactory>(c =>
				new OrmLiteConnectionFactory(
					appConfig.ConnectionString.MapHostAbsolutePath(),
					SqliteOrmLiteDialectProvider.Instance));

			ConfigureDatabase.Init(container.Resolve<IDbConnectionFactory>());


			//register different cache implementations depending on availability
			const bool hasRedis = false;
			if (hasRedis)
				container.Register<ICacheClient>(c => new BasicRedisClientManager());
			else
				container.Register<ICacheClient>(c => new MemoryCacheClient());


			log.InfoFormat("AppHost Configured: " + DateTime.Now);
		}
	}

}