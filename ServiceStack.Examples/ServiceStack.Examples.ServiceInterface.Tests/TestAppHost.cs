using System;
using Moq;
using ServiceStack.CacheAccess.Providers;
using ServiceStack.Configuration;
using ServiceStack.DataAccess;
using ServiceStack.Logging;
using ServiceStack.LogicFacade;
using ServiceStack.Service;
using ServiceStack.ServiceInterface;
using ServiceStack.WebHost.Endpoints;

namespace ServiceStack.Examples.ServiceInterface.Tests
{
	public class TestAppHost : EndpointHostBase, IDisposable
	{
		public static TestAppHost Instance;

		/// <summary>
		/// Configure this applicaiton instance.  
		/// Called by Application_Start() in Global.asax 
		/// </summary>
		public static void Init()
		{
			if (Instance == null)
			{
				Instance = new TestAppHost();
			}
		}

		private TestAppHost()
		{
			var factory = new FactoryProvider();

			ApplicationContext.SetInstanceContext(
				new BasicApplicationContext(factory, new MemoryCacheClient(), new ConfigurationResourceManager()));

			base.SetConfig(new EndpointHostConfig {
				ServiceName = "TestAppHost",
				ServiceController = new ServiceController(new PortResolver(typeof(GetFactorialHandler).Assembly)),
			});


			var log = LogManager.GetLogger(GetType());
			log.InfoFormat("TestAppHost Created: " + DateTime.Now);
		}

		protected override IOperationContext CreateOperationContext(object requestDto, EndpointAttributes endpointAttributes)
		{
			var requestContext = new RequestContext(requestDto, endpointAttributes, new FactoryProvider(FactoryUtils.ObjectFactory));
			return new OperationContext(ApplicationContext.Instance, requestContext);
		}

		public void Dispose()
		{
			ApplicationContext.Instance.Factory.Dispose();
		}
	}
}