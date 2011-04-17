using System.IO;
using System.Text;
using NUnit.Framework;
using ServiceStack.Common.Web;
using ServiceStack.ServiceInterface.Testing;
using ServiceStack.Text;
using ServiceStack.WebHost.Endpoints.Extensions;
using ServiceStack.WebHost.Endpoints.Tests.Mocks;
using ServiceStack.WebHost.Endpoints.Tests.Support;

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

			var httpResult = new HttpResult(customText, ContentType.Html)
			{
				Headers =
				{
					{"X-Custom","Header"}
				}
			};

			var reponseWasAutoHandled = mockResponse.WriteToResponse(httpResult, ContentType.Html);

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


			var httpResult = new HttpResult(ms, ContentType.Html)
			{
				Headers =
				{
					{"X-Custom","Header"}
				}
			};

			var reponseWasAutoHandled = mockResponse.WriteToResponse(httpResult, ContentType.Html);

			Assert.That(reponseWasAutoHandled, Is.True);

			var writtenString = mockResponse.GetOutputStreamAsString();
			Assert.That(writtenString, Is.EqualTo(customText));
			Assert.That(mockResponse.Headers["X-Custom"], Is.EqualTo("Header"));
		}

	}

}