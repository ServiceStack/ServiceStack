using System.ServiceModel;
using NUnit.Framework;
using ServiceStack.UsageExamples.Support;
using proxy = ServiceStack.UsageExamples.BasicHttpClientProxy;

namespace ServiceStack.UsageExamples
{
	/// <summary>
	/// Examples using the proxy client generated when you 'Add Service Reference' from visual studio.
	/// </summary>

	[TestFixture]
	public class UsingBasicHttpServiceReferenceClient : TestBase
	{
		[Test]
		public void Get_customers_using_generated_service_reference_proxy()
		{
			var binding = new BasicHttpBinding();
			var version = 100;

			using (var client = new proxy.SyncReplyClient(binding, new EndpointAddress(BasicHttpSyncReplyUri)))
			{
				var request = new proxy.ArrayOfIntId { base.CustomerId };
				var properties = new proxy.Properties();
				proxy.ResponseStatus responseStatus;
				var customers = client.GetCustomers(request, ref properties, ref version, out responseStatus);
				Assert.AreEqual(1, customers.Length);
				Assert.AreEqual(CustomerId, customers[0].Id);
			}
		}

	}
}