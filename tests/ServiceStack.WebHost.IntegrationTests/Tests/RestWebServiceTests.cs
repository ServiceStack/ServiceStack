using System;
using System.IO;
using System.Net;
using NUnit.Framework;
using ServiceStack.Common.Web;
using ServiceStack.Service;
using ServiceStack.Text;
using ServiceStack.WebHost.Endpoints;
using ServiceStack.WebHost.IntegrationTests.Operations;

namespace ServiceStack.WebHost.IntegrationTests.Tests
{
	[TestFixture]
	public class RestWebServiceTests
		: TestsBase
	{
		readonly EndpointHostConfig defaultConfig = new EndpointHostConfig();

		public HttpWebResponse GetWebResponse(string uri, string acceptContentTypes)
		{
			var webRequest = (HttpWebRequest)WebRequest.Create(uri);
			webRequest.Accept = acceptContentTypes;
			return (HttpWebResponse)webRequest.GetResponse();
		}

		public string GetContents(WebResponse webResponse)
		{
			using (var stream = webResponse.GetResponseStream())
			{
				var contents = new StreamReader(stream).ReadToEnd();
				return contents;
			}
		}

		public void AssertResponse(HttpWebResponse response, string contentType)
		{
			Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
			Assert.That(response.ContentType.StartsWith(contentType));
		}

		public void AssertResponse<T>(HttpWebResponse response, string contentType, Action<T> customAssert)
		{
			contentType = contentType ?? defaultConfig.DefaultContentType;

			AssertResponse(response, contentType);
			var contents = GetContents(response);

			T result;
			switch (contentType)
			{
				case ContentType.Xml:
					result = XmlSerializer.DeserializeFromString<T>(contents);
					break;

				case ContentType.Json:
					result = JsonSerializer.DeserializeFromString<T>(contents);
					break;

				case ContentType.Jsv:
					result = TypeSerializer.DeserializeFromString<T>(contents);
					break;

				default:
					throw new NotSupportedException(response.ContentType);
			}

			customAssert(result);
		}

		[Test]
		public void Can_call_simple_EchoRequest_with_AcceptAll()
		{
			var response = GetWebResponse(ServiceClientBaseUri + "/echo/1/One", "*/*");
			AssertResponse<EchoRequest>(response, null, x =>
			{
				Assert.That(x.Id, Is.EqualTo(1));
				Assert.That(x.String, Is.EqualTo("One"));
			});
		}

		[Test]
		public void Can_call_simple_EchoRequest_with_AcceptJson()
		{
			var response = GetWebResponse(ServiceClientBaseUri + "/echo/1/One", ContentType.Json);
			AssertResponse<EchoRequest>(response, ContentType.Json, x =>
			{
				Assert.That(x.Id, Is.EqualTo(1));
				Assert.That(x.String, Is.EqualTo("One"));
			});
		}

		[Test]
		public void Can_call_simple_EchoRequest_with_AcceptXml()
		{
			var response = GetWebResponse(ServiceClientBaseUri + "/echo/1/One", ContentType.Xml);
			AssertResponse<EchoRequest>(response, ContentType.Xml, x =>
			{
				Assert.That(x.Id, Is.EqualTo(1));
				Assert.That(x.String, Is.EqualTo("One"));
			});
		}

		[Test]
		public void Can_call_simple_EchoRequest_with_AcceptJsv()
		{
			var response = GetWebResponse(ServiceClientBaseUri + "/echo/1/One", ContentType.Jsv);
			AssertResponse<EchoRequest>(response, ContentType.Jsv, x =>
			{
				Assert.That(x.Id, Is.EqualTo(1));
				Assert.That(x.String, Is.EqualTo("One"));
			});
		}

		[Test]
		public void Can_call_simple_EchoRequest_with_QueryString()
		{
			var response = GetWebResponse(ServiceClientBaseUri + "/echo/1/One?Long=2&Bool=True", ContentType.Json);
			AssertResponse<EchoRequest>(response, ContentType.Json, x =>
			{
				Assert.That(x.Id, Is.EqualTo(1));
				Assert.That(x.String, Is.EqualTo("One"));
				Assert.That(x.Long, Is.EqualTo(2));
				Assert.That(x.Bool, Is.EqualTo(true));
			});
		}

		protected override IServiceClient CreateNewServiceClient()
		{
			throw new NotImplementedException();
		}
	}

}