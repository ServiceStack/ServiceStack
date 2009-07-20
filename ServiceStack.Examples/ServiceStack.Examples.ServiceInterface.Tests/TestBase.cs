using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
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
			return new OperationContext(ApplicationContext.Instance, new RequestContext(request, new FactoryProvider()));
		}

	}
}
