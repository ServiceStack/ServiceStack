using Moq;
using @ServiceModelNamespace@;
using ServiceStack.CacheAccess.Providers;
using ServiceStack.Configuration;
using ServiceStack.DataAccess;
using ServiceStack.ServiceInterface;
using ServiceStack.WebHost.Endpoints;

namespace @ServiceNamespace@.Tests.Support
{
	public class TestBase
	{
		protected ApplicationContext AppContext { get; private set; }

		public TestBase()
		{
			// Setup the application context
			this.AppContext = new ApplicationContext {
				Cache = new MemoryCacheClient(),
				Factory = new FactoryProvider(null),
				Resources = new ConfigurationResourceManager(),				
			};
		}

		protected void RegisterPersistenceProvider(IPersistenceProvider provider)
		{
			var manager = new Mock<IPersistenceProviderManager>();
			manager.Expect(m => m.CreateProvider()).Returns(provider);
			this.AppContext.Factory.Register(manager.Object);
		}

		protected virtual OperationContext CreateOperationContext(object requestDto, params object[] providers)
		{
			return new OperationContext(this.AppContext, new RequestContext(requestDto, new FactoryProvider(null, providers)));
		}

		protected virtual OperationContext CreateOperationContext(string xml, params object[] providers)
		{
			var requestDto = new XmlRequestDto(xml, ServiceModelFinder.Instance);
			return new OperationContext(this.AppContext, new RequestContext(requestDto, new FactoryProvider(null, providers)));
		}
	}
}