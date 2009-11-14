using System;
using System.Reflection;
using Funq;
using RemoteInfo.ServiceInterface;
using ServiceStack.Configuration;
using ServiceStack.Logging;
using ServiceStack.Logging.Support.Logging;
using ServiceStack.WebHost.Endpoints;

namespace RemoteInfo.Host.Web
{
	/// <summary>
	/// Web Services with http://www.servicestack.net/
	/// 
	/// Configuring ServiceStack to run inside a webserver.
	/// </summary>
	public class AppHost 
		: AppHostBase
	{
		private static ILog log;

		public AppHost() 
			: base("MonoTouch RemoteInfo", typeof(GetDirectoryInfoHandler).Assembly)
		{
			LogManager.LogFactory = new DebugLogFactory();
			log = LogManager.GetLogger(GetType());
		}

		public override void Configure(Container container)
		{
			container.Register<IResourceManager>(new ConfigurationResourceManager());
			container.Register(c => new RemoteInfoConfig(c.Resolve<IResourceManager>()));

			log.InfoFormat("AppHost Configured: " + DateTime.Now);
		}
	}
}