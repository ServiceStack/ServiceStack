using System.IO;
using NUnit.Framework;
using ServiceStack.ServiceHost;
using ServiceStack.ServiceInterface.Testing;
using ServiceStack.Text;
using ServiceStack.Web;
using ServiceStack.WebHost.Endpoints.Tests.Mocks;

namespace ServiceStack.WebHost.Endpoints.Tests
{
	[TestFixture]
	public class HttpResultTests : TestBase
	{
		protected override void Configure(Funq.Container container) { }

		[Test]
		public void Can_send_ResponseText_test_with_Custom_Header()
		{
			var mockResponse = new HttpResponseMock();

			var customText = "<h1>Custom Text</h1>";

            var httpResult = new HttpResult(customText, MimeTypes.Html)
            {
				Headers =
				{
					{"X-Custom","Header"}
				}
			};

            var reponseWasAutoHandled = mockResponse.WriteToResponse(httpResult, MimeTypes.Html);

			Assert.That(reponseWasAutoHandled, Is.True);

			var writtenString = mockResponse.GetOutputStreamAsString();
			Assert.That(writtenString, Is.EqualTo(customText));
			Assert.That(mockResponse.Headers["X-Custom"], Is.EqualTo("Header"));
		}

		[Test]
		public void Can_send_ResponseStream_test_with_Custom_Header()
		{
			var mockResponse = new HttpResponseMock();

			var customText = "<h1>Custom Stream</h1>";
			var customTextBytes = customText.ToUtf8Bytes();
			var ms = new MemoryStream();
			ms.Write(customTextBytes, 0, customTextBytes.Length);


            var httpResult = new HttpResult(ms, MimeTypes.Html)
            {
				Headers =
				{
					{"X-Custom","Header"}
				}
			};

            var reponseWasAutoHandled = mockResponse.WriteToResponse(httpResult, MimeTypes.Html);

			Assert.That(reponseWasAutoHandled, Is.True);

			var writtenString = mockResponse.GetOutputStreamAsString();
			Assert.That(writtenString, Is.EqualTo(customText));
			Assert.That(mockResponse.Headers["X-Custom"], Is.EqualTo("Header"));
		}

		[Test]
		public void Can_send_ResponseText_test_with_StatusDescription()
		{
            var mockRequest = new MockHttpRequest { ContentType = MimeTypes.Json };
			var mockRequestContext = new HttpRequestContext(mockRequest, null, new object());
			var mockResponse = new HttpResponseMock();

			var customStatus = "Custom Status Description";

			var httpResult = new HttpResult(System.Net.HttpStatusCode.Accepted, customStatus) {
				RequestContext = mockRequestContext
			};

            var reponseWasAutoHandled = mockResponse.WriteToResponse(httpResult, MimeTypes.Html);

			Assert.That(reponseWasAutoHandled, Is.True);

			var statusDesc = mockResponse.StatusDescription;
			Assert.That(mockResponse.StatusCode, Is.EqualTo((int)System.Net.HttpStatusCode.Accepted));
			Assert.That(statusDesc, Is.EqualTo(customStatus));
		}

		[Test]
		public void Can_handle_null_HttpResult_StatusDescription()
		{
			var mockResponse = new HttpResponseMock();

			var httpResult = new HttpResult();
			httpResult.StatusDescription = null;

            mockResponse.WriteToResponse(httpResult, MimeTypes.Html);

			Assert.IsNotNull(mockResponse.StatusDescription);
		}
	}

}