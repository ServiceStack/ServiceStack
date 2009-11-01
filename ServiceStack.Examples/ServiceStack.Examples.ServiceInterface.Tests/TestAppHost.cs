using System;
using System.Reflection;
using Funq;
using ServiceStack.Configuration;
using ServiceStack.Logging;
using ServiceStack.Logging.Support.Logging;
using ServiceStack.WebHost.Endpoints;

namespace ServiceStack.Examples.ServiceInterface.Tests
{
	public class TestAppHost 
		: EndpointHostBase
	{
		private static ILog log;

		public Container Container { get; set; }

		public TestAppHost(string serviceName, params Assembly[] assembliesWithServices) 
			: base(serviceName, assembliesWithServices)
		{
			LogManager.LogFactory = new ConsoleLogFactory();
			log = LogManager.GetLogger(GetType());
		}

		public override void Configure(Container container)
		{
			this.Container = container;

			container.Register<IResourceManager>(c => new ConfigurationResourceManager());

			log.InfoFormat("TestAppHost Created: " + DateTime.Now);
		}

		public static void Reset()
		{
			Instance = null;
		}
	}
}