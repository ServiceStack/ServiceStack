using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Moq;
using NUnit.Framework;
using ServiceStack.DataAccess;
using ServiceStack.ServiceInterface;

namespace ServiceStack.Examples.ServiceInterface.Tests
{
	[TestFixture]
	public class TestBase
	{
		[TestFixtureSetUp]
		public void SetUp()
		{
			TestAppHost.Init();
		}

		protected OperationContext CreateOperationContext(object request)
		{
			return new OperationContext(ApplicationContext.Instance, 
				new RequestContext(request));
		}

		protected IPersistenceProviderManager GetMockProviderManagerObject(Mock<IQueryablePersistenceProvider> mockPersistence)
		{
			var mockProviderManager = new Mock<IPersistenceProviderManager>();
			mockProviderManager.Expect(x => x.GetProvider()).Returns(mockPersistence.Object);
			return mockProviderManager.Object;
		}
	}
}
