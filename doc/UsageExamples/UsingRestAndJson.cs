using System.IO;
using System.Net;
using NUnit.Framework;
using Sakila.ServiceModel.Version100.Types;
using ServiceStack.ServiceModel.Serialization;
using ServiceStack.UsageExamples.Support;
using DtoOperations = Sakila.ServiceModel.Version100.Operations.SakilaService;

namespace ServiceStack.UsageExamples
{
	[TestFixture]
	public class UsingRestAndJson : TestBase
	{
		[Test]
		public void Get_customers_using_rest_and_json()
		{
			var requestUri = string.Format("{0}/{1}?CustomerIds={2}",
				JsonSyncReplyBaseUri, typeof(DtoOperations.GetCustomers).Name, CustomerId);

			var client = WebRequest.Create(requestUri);
			var json = new StreamReader(client.GetResponse().GetResponseStream()).ReadToEnd();
			var response = JsonDataContractDeserializer.Instance.Parse(json,
				typeof(DtoOperations.GetCustomersResponse)) as DtoOperations.GetCustomersResponse;

			Assert.IsNotNull(response);
			Assert.AreEqual(1, response.Customers.Count);
			Assert.AreEqual(CustomerId, response.Customers[0].Id);
		}

		[Test]
		public void Get_customers_using_json_post()
		{
			var request = new DtoOperations.GetCustomers { CustomerIds = new ArrayOfIntId(new[] { base.CustomerId }), };
			var jsonRequest = JsonDataContractSerializer.Instance.Parse(request);

			var requestUri = JsonSyncReplyBaseUri + "/" + typeof(DtoOperations.GetCustomers).Name;
			var client = WebRequest.Create(requestUri);
			client.Method = "POST";
			client.ContentType = "application/json";
			using (var writer = new StreamWriter(client.GetRequestStream()))
			{
				writer.Write(jsonRequest);
			}

			var json = new StreamReader(client.GetResponse().GetResponseStream()).ReadToEnd();
			var response = JsonDataContractDeserializer.Instance.Parse(json,
				typeof(DtoOperations.GetCustomersResponse)) as DtoOperations.GetCustomersResponse;

			Assert.IsNotNull(response);
			Assert.AreEqual(1, response.Customers.Count);
			Assert.AreEqual(CustomerId, response.Customers[0].Id);
		}

	}
}