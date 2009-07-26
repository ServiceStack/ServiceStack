using ServiceStack.CacheAccess.Providers;
using ServiceStack.Configuration;
using ServiceStack.DataAccess;
using ServiceStack.DataAccess.Db4oProvider;
using ServiceStack.LogicFacade;
using ServiceStack.Service;
using ServiceStack.ServiceInterface;

namespace ServiceStack.Examples.ServiceInterface.Tests.MonoTests
{
	public class TestProgramBase
	{
		protected static IOperationContext CreateOperationContext(object requestDto)
		{
			var requestContext = new RequestContext(requestDto, EndpointAttributes.None, new FactoryProvider());
			return new OperationContext(ApplicationContext.Instance, requestContext);
		}

		protected static void InitApplicationContext()
		{
			var factory = new FactoryProvider();

			factory.Register<IPersistenceProviderManager>(
				new Db4oFileProviderManager("test.db4o"));

			ApplicationContext.SetInstanceContext(
				new BasicApplicationContext(factory, new MemoryCacheClient(), new ConfigurationResourceManager()));
		}
	}

}