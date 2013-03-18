using System;
using System.IO;
using System.Net;
using NUnit.Framework;
using ServiceStack.Common;
using ServiceStack.Common.Web;
using ServiceStack.ServiceClient.Web;
using ServiceStack.ServiceHost;
using ServiceStack.Text;
using ServiceStack.WebHost.IntegrationTests.Services;

namespace ServiceStack.WebHost.IntegrationTests.Tests
{
	[Route("/rawrequest")]
	public class RawRequest : IRequiresRequestStream
	{
		public Stream RequestStream { get; set; }
	}

	public class RawRequestResponse
	{
		public string Result { get; set; }
	}

	public class RawRequestService : IService<RawRequest>
	{
		public object Execute(RawRequest request)
		{
			var rawRequest = request.RequestStream.ToUtf8String();
			return new RawRequestResponse { Result = rawRequest };
		}
	}

	[TestFixture]
	public class RawRequestTests 
	{
		[Test]
		public void Can_POST_raw_request()
		{
			var rawData = "<<(( 'RAW_DATA' ))>>";
			var requestUrl = Config.ServiceStackBaseUri + "/rawrequest";
            var json = requestUrl.PutStringToUrl(rawData, contentType: ContentType.PlainText, acceptContentType: ContentType.Json);
			var response = json.FromJson<RawRequestResponse>();
			Assert.That(response.Result, Is.EqualTo(rawData));
		}

		[Test]
		public void Can_PUT_raw_request()
		{
			var rawData = "<<(( 'RAW_DATA' ))>>";
			var requestUrl = Config.ServiceStackBaseUri + "/rawrequest";
            var json = requestUrl.PutStringToUrl(rawData, contentType: ContentType.PlainText, acceptContentType: ContentType.Json);
			var response = json.FromJson<RawRequestResponse>();
			Assert.That(response.Result, Is.EqualTo(rawData));
		}

	}

}