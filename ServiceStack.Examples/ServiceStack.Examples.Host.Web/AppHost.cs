using System;
using Funq;
using ServiceStack.Common.Utils;
using ServiceStack.Configuration;
using ServiceStack.DataAccess;
using ServiceStack.DataAccess.Db4oProvider;
using ServiceStack.Examples.ServiceInterface;
using ServiceStack.Logging;
using ServiceStack.Logging.Support.Logging;
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

			var config = container.Resolve<IResourceManager>();

			container.Register<IPersistenceProviderManager>(
				new Db4OFileProviderManager(config.GetString("Db4oConnectionString").MapHostAbsolutePath()));

			log.InfoFormat("AppHost Configured: " + DateTime.Now);
		}
	}

}