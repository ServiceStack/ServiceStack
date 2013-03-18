using System;
using System.IO;
using System.Net;
using System.Web;
using NUnit.Framework;
using ServiceStack.Common;
using ServiceStack.Common.Web;
using ServiceStack.ServiceClient.Web;
using ServiceStack.Text;
using ServiceStack.WebHost.Endpoints.Tests.Support.Host;
using ServiceStack.WebHost.Endpoints.Tests.Support.Operations;

namespace ServiceStack.WebHost.Endpoints.Tests
{
	[TestFixture]
	public class CustomRequestDataTests
	{
		private const string ListeningOn = "http://localhost:82/";

		ExampleAppHostHttpListener appHost;
		readonly JsonServiceClient client = new JsonServiceClient(ListeningOn);
		private string customUrl = ListeningOn.CombineWith("customrequestbinder");
		private string predefinedUrl = ListeningOn.CombineWith("json/syncreply/customrequestbinder");

		[TestFixtureSetUp]
		public void OnTestFixtureSetUp()
		{
			appHost = new ExampleAppHostHttpListener();
			appHost.Init();
			appHost.Start(ListeningOn);
		}

		[TestFixtureTearDown]
		public void OnTestFixtureTearDown()
		{
			appHost.Dispose();
		}

		/// <summary>
		/// first-name=tom&item-0=blah&item-1-delete=1
		/// </summary>
		[Test]
		public void Can_parse_custom_form_data()
		{
			var webReq = (HttpWebRequest)WebRequest.Create("http://localhost:82/customformdata?format=json");
			webReq.Method = HttpMethods.Post;
			webReq.ContentType = ContentType.FormUrlEncoded;

			try
			{
				using (var sw = new StreamWriter(webReq.GetRequestStream()))
				{
					sw.Write("&first-name=tom&item-0=blah&item-1-delete=1");
				}
				var response = new StreamReader(webReq.GetResponse().GetResponseStream()).ReadToEnd();

				Assert.That(response, Is.EqualTo("{\"FirstName\":\"tom\",\"Item0\":\"blah\",\"Item1Delete\":\"1\"}"));
			}
			catch (WebException webEx)
			{
				var errorWebResponse = ((HttpWebResponse)webEx.Response);
				var errorResponse = new StreamReader(errorWebResponse.GetResponseStream()).ReadToEnd();

				Assert.Fail(errorResponse);
			}
		}

		[Test]
		public void Does_use_request_binder_for_GET()
		{
			var response = client.Get<CustomRequestBinderResponse>("/customrequestbinder");
			Assert.That(response.FromBinder);
		}

		[Test]
		public void Does_use_request_binder_for_predefined_GET()
		{
            var responseStr = predefinedUrl.GetJsonFromUrl();
			Console.WriteLine(responseStr);
			var response = responseStr.FromJson<CustomRequestBinderResponse>();
			Assert.That(response.FromBinder);
		}

		[Test]
		public void Does_use_request_binder_for_predefined_GET_with_QueryString()
		{
			var customUrlWithQueryString = customUrl + "?IsFromBinder=false";
			var responseStr = customUrlWithQueryString.GetJsonFromUrl();
			Console.WriteLine(responseStr);
			var response = responseStr.FromJson<CustomRequestBinderResponse>();
			Assert.That(response.FromBinder);
		}

		[Test]
		public void Does_use_request_binder_for_Send()
		{
			var response = client.Send<CustomRequestBinderResponse>(new CustomRequestBinder());
			Assert.That(response.FromBinder);
		}

		[Test]
		public void Does_use_request_binder_for_POST()
		{
			var response = client.Post<CustomRequestBinderResponse>("/customrequestbinder", new CustomRequestBinder());
			Assert.That(response.FromBinder);
		}

		[Test]
		public void Does_use_request_binder_for_POST_FormData()
		{
			var responseStr = customUrl.PostToUrl("IsFromBinder=false", acceptContentType:ContentType.Json);
			Console.WriteLine(responseStr);
			var response = responseStr.FromJson<CustomRequestBinderResponse>();
			Assert.That(response.FromBinder);
		}

		[Test]
		public void Does_use_request_binder_for_POST_FormData_without_ContentType()
		{
			var responseStr = customUrl.PostJsonToUrl("{\"IsFromBinder\":false}");
			Console.WriteLine(responseStr);
			var response = responseStr.FromJson<CustomRequestBinderResponse>();
			Assert.That(response.FromBinder);
		}

		[Test]
		public void Does_use_request_binder_for_POST_FormData_without_ContentType_with_QueryString()
		{
			string customUrlWithQueryString = customUrl + "?IsFromBinder=false";
			var responseStr = customUrlWithQueryString.PostToUrl("k=v", acceptContentType: ContentType.Json);
			var response = responseStr.FromJson<CustomRequestBinderResponse>();
			Assert.That(response.FromBinder);
		}

		[Test]
		public void Does_use_request_binder_for_predefined_POST()
		{
			var responseStr = predefinedUrl.PostJsonToUrl(new CustomRequestBinder());
			Console.WriteLine(responseStr);
			var response = responseStr.FromJson<CustomRequestBinderResponse>();
			Assert.That(response.FromBinder);
		}

		[Test]
		public void Does_use_request_binder_for_predefined_POST_FormData()
		{
			var responseStr = predefinedUrl.PostToUrl("k=v", acceptContentType: ContentType.Json);
			Console.WriteLine(responseStr);
			var response = responseStr.FromJson<CustomRequestBinderResponse>();
			Assert.That(response.FromBinder);
		}

		[Test]
		public void Does_use_request_binder_for_PUT()
		{
			var response = client.Put<CustomRequestBinderResponse>("/customrequestbinder", new CustomRequestBinder());
			Assert.That(response.FromBinder);
		}

		[Test]
		public void Does_use_request_binder_for_DELETE()
		{
			var response = client.Delete<CustomRequestBinderResponse>("/customrequestbinder");
			Assert.That(response.FromBinder);
		}

	}

}
