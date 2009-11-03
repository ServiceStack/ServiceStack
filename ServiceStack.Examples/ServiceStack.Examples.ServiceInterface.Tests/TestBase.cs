using Funq;
using Moq;
using NUnit.Framework;
using ServiceStack.DataAccess;

namespace ServiceStack.Examples.ServiceInterface.Tests
{
	[TestFixture]
	public class TestBase
	{
		[TestFixtureSetUp]
		public void SetUp()
		{
			TestAppHost.Reset();

			var appHost = new TestAppHost("TestAppHost", typeof (GetFactorialService).Assembly);
			appHost.Init();
		}

		protected Container Container
		{
			get
			{
				return ((TestAppHost)TestAppHost.Instance).Container;
			}
		}

		protected IPersistenceProviderManager GetMockProviderManagerObject(Mock<IQueryablePersistenceProvider> mockPersistence)
		{
			var mockProviderManager = new Mock<IPersistenceProviderManager>();
			mockProviderManager.Expect(x => x.GetProvider()).Returns(mockPersistence.Object);
			return mockProviderManager.Object;
		}
	}
}
