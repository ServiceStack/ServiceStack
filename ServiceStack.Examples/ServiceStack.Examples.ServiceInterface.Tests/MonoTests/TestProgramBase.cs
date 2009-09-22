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
			return new OperationContext(ApplicationContext.Instance,
				new RequestContext(requestDto));
		}

		protected static void InitApplicationContext()
		{
			var factory = new FactoryProvider();

			ApplicationContext.SetInstanceContext(
				new BasicApplicationContext(factory, new MemoryCacheClient(), new ConfigurationResourceManager()));
		}
	}

}