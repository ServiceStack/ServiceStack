using Moq;
using ServiceStack.CacheAccess.Providers;
using ServiceStack.Configuration;
using ServiceStack.DataAccess;
using ServiceStack.ServiceInterface;
using ServiceStack.WebHost.Endpoints;
using @ServiceNamespace@.ServiceInterface;
using @ServiceModelNamespace@;

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
			manager.Expect(m => m.GetProvider()).Returns(provider);
			this.AppContext.Factory.Register(manager.Object);
		}

		protected virtual @DatabaseName@OperationContext CreateOperationContext(object requestDto, params object[] providers)
		{
			return new @DatabaseName@OperationContext(this.AppContext, new RequestContext(requestDto, new FactoryProvider(null, providers)));
		}

		protected virtual @DatabaseName@OperationContext CreateOperationContext(string xml, params object[] providers)
		{
			var requestDto = new XmlRequestDto(xml, ServiceModelFinder.Instance);
			return new @DatabaseName@OperationContext(this.AppContext, new RequestContext(requestDto, new FactoryProvider(null, providers)));
		}
	}
}