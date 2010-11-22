using System.IO;
using System.Linq;
using System.Net;
using System.Xml.Linq;
using NUnit.Framework;
using Sakila.ServiceModel.Version100.Operations.SakilaService;
using ServiceStack.ServiceModel.Extensions;
using ServiceStack.ServiceModel.Serialization;
using ServiceStack.UsageExamples.Support;

namespace ServiceStack.UsageExamples
{
	[TestFixture]
	public class UsingRawHttpClient : TestBase
	{
		[Test]
		public void Get_customers_using_soap12_http_post()
		{
			var soapRequest =
                 @"<s:Envelope xmlns:s=""http://www.w3.org/2003/05/soap-envelope"" xmlns:a=""http://www.w3.org/2005/08/addressing"">
                    <s:Header>
                        <a:Action s:mustUnderstand=""1"">http://services.servicestack.net/GetCustomers</a:Action>
                        <a:To s:mustUnderstand=""1"">{0}</a:To>
                    </s:Header>
                    <s:Body>
                        <GetCustomers xmlns=""http://schemas.servicestack.net/types/"">
                            <CustomerIds>
	                            <Id>{1}</Id>
                            </CustomerIds>
                            <Version>100</Version>
                        </GetCustomers>
                    </s:Body>
                </s:Envelope>";

			var request = string.Format(soapRequest, WsSyncReplyUri, CustomerId);
			var client = (HttpWebRequest)WebRequest.Create(base.WsSyncReplyUri);
			client.ContentType = "application/soap+xml; charset=utf-8";
			client.Accept = "text/xml";
			client.Method = "POST";

			using (var stream = client.GetRequestStream())
			{
				using (var writer = new StreamWriter(stream))
				{
					writer.Write(request);
				}
			}

			var soapResponse = new StreamReader(client.GetResponse().GetResponseStream()).ReadToEnd();
			var el = XElement.Parse(soapResponse);
			var customers = el.AnyElement("Body").AnyElement("GetCustomersResponse")
				 .AnyElement("Customers").AllElements("Customer").ToList();

			Assert.AreEqual(1, customers.Count);
			Assert.AreEqual(CustomerId, customers[0].GetInt("Id"));
		}

		[Test]
		public void Get_customers_using_soap11_http_post()
		{
			var soapRequest =
                 @"<?xml version=""1.0"" encoding=""utf-8""?>
                    <soap:Envelope xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"" xmlns:soap=""http://schemas.xmlsoap.org/soap/envelope/"">
                        <soap:Body>

                        <GetCustomers xmlns=""http://schemas.servicestack.net/types/"">
                            <CustomerIds>
	                            <Id>{0}</Id>
                            </CustomerIds>
                            <Version>100</Version>
                        </GetCustomers>

                       </soap:Body>
                    </soap:Envelope>";

			var request = string.Format(soapRequest, CustomerId);
			var client = (HttpWebRequest)WebRequest.Create(base.BasicHttpSyncReplyUri);
			client.ContentType = "text/xml; charset=utf-8";
			client.Headers.Add("SOAPAction", "GetCustomers");
			client.Accept = "text/xml";
			client.Method = "POST";

			using (var stream = client.GetRequestStream())
			{
				using (var writer = new StreamWriter(stream))
				{
					writer.Write(request);
				}
			}

			var soapResponse = new StreamReader(client.GetResponse().GetResponseStream()).ReadToEnd();
			var el = XElement.Parse(soapResponse);
			var customers = el.AnyElement("Body").AnyElement("GetCustomersResponse")
				 .AnyElement("Customers").AllElements("Customer").ToList();

			Assert.AreEqual(1, customers.Count);
			Assert.AreEqual(CustomerId, customers[0].GetInt("Id"));
		}

		[Test]
		public void Get_customers_using_xml_http_post()
		{
			var xmlRequest = string.Format(
			  @"<GetCustomers xmlns=""http://schemas.servicestack.net/types/"">
                    <CustomerIds>
                        <Id>{0}</Id>
                    </CustomerIds>
                    <Version>100</Version>
                </GetCustomers>", CustomerId);

			var requestUri = base.XmlSyncReplyBaseUri + "/GetCustomers";
			var client = WebRequest.Create(requestUri);
			client.Method = "POST";
			client.ContentType = "application/xml";
			using (var writer = new StreamWriter(client.GetRequestStream()))
			{
				writer.Write(xmlRequest);
			}

			var xmlResponse = new StreamReader(client.GetResponse().GetResponseStream()).ReadToEnd();
			var el = XElement.Parse(xmlResponse);
			var customers = el.AnyElement("Customers").AllElements("Customer").ToList();

			Assert.AreEqual(1, customers.Count);
			Assert.AreEqual(CustomerId, customers[0].GetInt("Id"));
		}

		[Test]
		public void Get_customers_using_json_http_post()
		{
			var jsonRequest = string.Format(@"{{""CustomerIds"":[{0}],""Version"":0}}", CustomerId);

			var requestUri = base.JsonSyncReplyBaseUri + "/GetCustomers";
			var client = WebRequest.Create(requestUri);
			client.Method = "POST";
			client.ContentType = "application/json";
			using (var writer = new StreamWriter(client.GetRequestStream()))
			{
				writer.Write(jsonRequest);
			}

			var jsonResponse = new StreamReader(client.GetResponse().GetResponseStream()).ReadToEnd();
			var response = JsonDataContractDeserializer.Instance.Parse(jsonResponse,
				typeof(GetCustomersResponse)) as GetCustomersResponse;

			Assert.IsNotNull(response);
			Assert.AreEqual(1, response.Customers.Count);
			Assert.AreEqual(CustomerId, response.Customers[0].Id);
		}

	}
}