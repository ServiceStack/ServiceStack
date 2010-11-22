using System.ServiceModel;
using NUnit.Framework;
using ServiceStack.UsageExamples.Support;

namespace ServiceStack.UsageExamples
{
	/// <summary>
	/// Examples using the proxy client generated from svcutil.exe
	/// </summary>
	[TestFixture]
	public class UsingSvcutilGeneratedClient : TestBase
	{
		[Test]
		public void Get_customers_using_generated_svcutil_proxy()
		{
			var binding = new WSHttpBinding();
			binding.Security.Mode = SecurityMode.None;
			var version = 100;

			using (var client = new svc.SyncReplyClient(binding, new EndpointAddress(WsSyncReplyUri)))
			{
				var request = new svc.ArrayOfIntId { CustomerId };
				var properties = new svc.Properties();
				svc.ResponseStatus responseStatus;
				var customers = client.GetCustomers(request, ref properties, ref version, out responseStatus);
				Assert.AreEqual(1, customers.Length);
				Assert.AreEqual(CustomerId, customers[0].Id);
			}
		}

		[Test]
		public void Get_customers_using_generated_svcutil_proxy_BasicHttp()
		{
			var binding = new BasicHttpBinding(BasicHttpSecurityMode.None);
			var version = 100;

			using (var client = new svc.SyncReplyClient(binding, new EndpointAddress(BasicHttpSyncReplyUri)))
			{
				var request = new svc.ArrayOfIntId { CustomerId };
				var properties = new svc.Properties();
				svc.ResponseStatus responseStatus;
				var customers = client.GetCustomers(request, ref properties, ref version, out responseStatus);
				Assert.AreEqual(1, customers.Length);
				Assert.AreEqual(CustomerId, customers[0].Id);
			}
		}

	}
}