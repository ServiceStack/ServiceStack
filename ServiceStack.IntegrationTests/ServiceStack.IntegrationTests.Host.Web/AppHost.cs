using System;
using Funq;
using ServiceStack.Configuration;
using ServiceStack.IntegrationTests.ServiceInterface;
using ServiceStack.Logging;
using ServiceStack.Logging.Support.Logging;
using ServiceStack.WebHost.Endpoints;

namespace ServiceStack.IntegrationTests.Host.Web
{
	/// <summary>
	/// An example of a AppHost to have your services running inside a webserver.
	/// </summary>
	public class AppHost
		: AppHostBase
	{
		private static ILog log;

		public AppHost()
			: base("ServiceStack IntegrationTests", typeof(PingService).Assembly)
		{
			LogManager.LogFactory = new DebugLogFactory();
			log = LogManager.GetLogger(typeof(AppHost));
		}

		public override void Configure(Container container)
		{
			container.Register<IResourceManager>(new ConfigurationResourceManager());

			var config = container.Resolve<IResourceManager>();

			log.InfoFormat("AppHost Configured: " + DateTime.Now);
		}
	}

}