using NUnit.Framework;
using Sakila.ServiceModel.Version100.Operations.SakilaService;
using Sakila.ServiceModel.Version100.Types;
using ServiceStack.Service;
using ServiceStack.ServiceClient.Web;
using ServiceStack.UsageExamples.Support;

namespace ServiceStack.UsageExamples
{
	[TestFixture]
	public class UsingServiceClients : TestBase
	{
		[Test]
		public void Get_customers_using_Soap12ServiceClient()
		{
			using (IServiceClient client = new Soap12ServiceClient(base.WsSyncReplyUri))
			{
				var request = new GetCustomers { CustomerIds = new ArrayOfIntId { CustomerId } };
				var response = client.Send<GetCustomersResponse>(request);

				Assert.AreEqual(1, response.Customers.Count);
				Assert.AreEqual(CustomerId, response.Customers[0].Id);
			}
		}

		[Test]
		public void Get_customers_using_Soap11ServiceClient()
		{
			using (IServiceClient client = new Soap11ServiceClient(base.BasicHttpSyncReplyUri))
			{
				var request = new GetCustomers { CustomerIds = new ArrayOfIntId { CustomerId } };
				var response = client.Send<GetCustomersResponse>(request);

				Assert.AreEqual(1, response.Customers.Count);
				Assert.AreEqual(CustomerId, response.Customers[0].Id);
			}
		}

		[Test]
		public void Get_customers_using_XmlServiceClient()
		{
			using (IServiceClient client = new XmlServiceClient(base.XmlSyncReplyBaseUri))
			{
				var request = new GetCustomers { CustomerIds = new ArrayOfIntId { CustomerId } };
				var response = client.Send<GetCustomersResponse>(request);

				Assert.AreEqual(1, response.Customers.Count);
				Assert.AreEqual(CustomerId, response.Customers[0].Id);
			}
		}

		[Test]
		public void Get_customers_using_JsonServiceClient()
		{
			using (IServiceClient client = new JsonServiceClient(base.JsonSyncReplyBaseUri))
			{
				var request = new GetCustomers { CustomerIds = new ArrayOfIntId { CustomerId } };
				var response = client.Send<GetCustomersResponse>(request);

				Assert.AreEqual(1, response.Customers.Count);
				Assert.AreEqual(CustomerId, response.Customers[0].Id);
			}
		}

	}
}