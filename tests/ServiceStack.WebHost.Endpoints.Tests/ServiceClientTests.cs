using NUnit.Framework;
using ServiceStack.WebHost.Endpoints.Tests.Support;
using ServiceStack.WebHost.Endpoints.Tests.Support.Host;
using ServiceStack.WebHost.Endpoints.Tests.Support.Operations;

namespace ServiceStack.WebHost.Endpoints.Tests
{
	[TestFixture]
	public class ServiceClientTests
		: ServiceClientTestBase
	{
		/// <summary>
		/// These tests require admin privillages
		/// </summary>
		/// <returns></returns>
		public override AppHostHttpListenerBase CreateListener()
		{
			return new TestAppHostHttpListener();
		}

		[Test]
		public void Can_GetCustomers()
		{
			var request = new GetCustomer { CustomerId = 5 };

			Send<GetCustomerResponse>(request,
				response => Assert.That(response.Customer.Id, Is.EqualTo(request.CustomerId)));
		}
	}

}