using System.IO;
using NUnit.Framework;
using ServiceStack.Server;
using ServiceStack.Text;

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

	public class RawRequestService : IService
	{
		public object Any(RawRequest request)
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
            var json = requestUrl.PutStringToUrl(rawData, contentType: MimeTypes.PlainText, accept: MimeTypes.Json);
			var response = json.FromJson<RawRequestResponse>();
			Assert.That(response.Result, Is.EqualTo(rawData));
		}

		[Test]
		public void Can_PUT_raw_request()
		{
			var rawData = "<<(( 'RAW_DATA' ))>>";
			var requestUrl = Config.ServiceStackBaseUri + "/rawrequest";
            var json = requestUrl.PutStringToUrl(rawData, contentType: MimeTypes.PlainText, accept: MimeTypes.Json);
			var response = json.FromJson<RawRequestResponse>();
			Assert.That(response.Result, Is.EqualTo(rawData));
		}

	}

}