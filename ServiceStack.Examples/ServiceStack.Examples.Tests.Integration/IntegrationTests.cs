using System.Collections.Generic;
using NUnit.Framework;
using NUnit.Framework.SyntaxHelpers;
using ServiceStack.Examples.ServiceInterface.Types;

namespace ServiceStack.Examples.Tests.Integration
{
	/// <summary>
	/// Thix fixture contains end-to-end integration test that hosts
	/// the Console AppHost on {BaseUrl} and sends each request to the different
	/// ServiceStack endpoints.
	/// 
	/// Admin user privillages are required to host the Console Host (HttpListener)
	/// </summary>
	public class IntegrationTests
		: IntegrationTestBase
	{

		[Test]
		public void Can_GetFactorial()
		{
			var request = new GetFactorial { ForNumber = 4 };

			SendToEachEndpoint<GetFactorialResponse>(request, response =>
				Assert.That(response.Result, Is.EqualTo(24)));
		}

		[Test]
		public void Can_GetFibonacciNumbers()
		{
			var request = new GetFibonacciNumbers { Skip = 3, Take = 5 };

			SendToEachEndpoint<GetFibonacciNumbersResponse>(request, response =>
				Assert.That(response.Results, Is.EquivalentTo(new List<long> { 5, 8, 13, 21, 34 })));
		}

		[Test]
		public void Can_GetNorthwindCustomerOrders()
		{
			var request = new GetNorthwindCustomerOrders { CustomerId = "TESTCUSTOMER" };

			SendToEachEndpoint<GetNorthwindCustomerOrdersResponse>(request, response =>
				Assert.That(response.CustomerOrders.Customer.Id, Is.EqualTo(request.CustomerId)));
		}

	}
}
