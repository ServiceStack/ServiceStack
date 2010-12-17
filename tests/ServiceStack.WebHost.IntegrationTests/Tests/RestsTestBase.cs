using System;
using System.IO;
using System.Net;
using NUnit.Framework;
using ServiceStack.Common.Web;
using ServiceStack.ServiceInterface.Testing;
using ServiceStack.Text;
using ServiceStack.WebHost.Endpoints;
using ServiceStack.WebHost.IntegrationTests.Services;

namespace ServiceStack.WebHost.IntegrationTests.Tests
{
	public class RestsTestBase
		: TestsBase
	{
		readonly EndpointHostConfig defaultConfig = new EndpointHostConfig();

		public RestsTestBase()
			: base("http://localhost/ServiceStack.WebHost.IntegrationTests/servicestack/", typeof(HelloService).Assembly)
		{
		}

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


	}
}