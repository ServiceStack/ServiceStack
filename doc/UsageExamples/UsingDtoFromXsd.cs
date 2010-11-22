using NUnit.Framework;
using ServiceStack.Common.Utils;
using ServiceStack.ServiceClient.Web;
using ServiceStack.UsageExamples.Support;

namespace ServiceStack.UsageExamples
{
	/// <summary>
	/// Examples using the generated dto from the xsd
	/// </summary>
	[TestFixture]
	public class UsingDtoFromXsd : TestBase
	{
		[Test]
		public void Get_customers_using_dto_from_xsd_and_deserialize()
		{
			using (var client = new Soap12ServiceClient(base.WsSyncReplyUri))
			{
				var request = new xsd.GetCustomers { Version = 100, CustomerIds = new xsd.ArrayOfIntId { CustomerId } };
				var response = client.Send(request);
				var customersResponse = response.GetBody<xsd.GetCustomersResponse>();
				Assert.AreEqual(1, customersResponse.Customers.Length);
				Assert.AreEqual(CustomerId, customersResponse.Customers[0].Id);
			}
		}

		[Test]
		public void Get_customers_using_dto_from_xsd_and_deserialize_BasicHttp()
		{
			using (var client = new Soap11ServiceClient(base.BasicHttpSyncReplyUri))
			{
				var request = new xsd.GetCustomers { Version = 100, CustomerIds = new xsd.ArrayOfIntId { CustomerId } };
				var response = client.Send(request);
				var customersResponse = response.GetBody<xsd.GetCustomersResponse>();
				Assert.AreEqual(1, customersResponse.Customers.Length);
				Assert.AreEqual(CustomerId, customersResponse.Customers[0].Id);
			}
		}

	}
}