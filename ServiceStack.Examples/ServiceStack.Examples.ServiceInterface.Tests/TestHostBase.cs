using System;
using Funq;
using Moq;
using NUnit.Framework;
using ServiceStack.Configuration;
using ServiceStack.DataAccess;
using ServiceStack.Logging;
using ServiceStack.Logging.Support.Logging;
using ServiceStack.WebHost.Endpoints;

namespace ServiceStack.Examples.ServiceInterface.Tests
{
	[TestFixture]
	public class TestHostBase
		: AppHostBase
	{
		protected const string InMemoryDb = ":memory:";
		private static ILog log;

		public TestHostBase()
			: base("TestAppHost", typeof(GetFactorialService).Assembly)
		{
			LogManager.LogFactory = new ConsoleLogFactory();
			log = LogManager.GetLogger(GetType());

			Instance = null;
			Init();
		}

		public override void Configure(Container container)
		{
			container.Register<IResourceManager>(c => new ConfigurationResourceManager());

			log.InfoFormat("TestAppHost Created: " + DateTime.Now);
		}

		protected IPersistenceProviderManager GetMockProviderManagerObject(Mock<IQueryablePersistenceProvider> mockPersistence)
		{
			var mockProviderManager = new Mock<IPersistenceProviderManager>();
			mockProviderManager.Expect(x => x.GetProvider()).Returns(mockPersistence.Object);
			return mockProviderManager.Object;
		}
	}
}
