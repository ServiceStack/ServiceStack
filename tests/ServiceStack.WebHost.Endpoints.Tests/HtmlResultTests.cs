using System;
using System.Text;
using Moq;
using NUnit.Framework;
using ServiceStack.Common.Web;
using ServiceStack.ServiceInterface.Testing;
using ServiceStack.WebHost.Endpoints.Extensions;
using ServiceStack.WebHost.Endpoints.Tests.Mocks;
using ServiceStack.WebHost.Endpoints.Tests.Support;

namespace ServiceStack.WebHost.Endpoints.Tests
{
	[TestFixture]
	public class HtmlResultMetadataTests : TestBase
	{
		protected override void Configure(Funq.Container container) {}

		public static class Html
		{
			public static HttpResult RedirectTo(string url)
			{
				var html = string.Format(
					"<html><head><meta http-equiv=\"refresh\" content=\"0;url={0}\"></head></html>",
					url);

				return new HttpResult(html, ContentType.Html) {
					Headers = { { "Location", url } },
				};
			}
		}

		[Test]
		public void Test_response_with_html_result()
		{
			var mockResponse = new HttpResponseMock();

			const string url = "http://www.servicestack.net";
			var htmlResult = Html.RedirectTo(url);

			var reponseWasAutoHandled = mockResponse.WriteToResponse(htmlResult, "text/xml");

			Assert.That(reponseWasAutoHandled, Is.True);

			var expectedOutput = string.Format(
				"<html><head><meta http-equiv=\"refresh\" content=\"0;url={0}\"></head></html>", url);

			var writtenString = mockResponse.GetOutputStreamAsString();
			Assert.That(writtenString, Is.EqualTo(expectedOutput));
			Assert.That(mockResponse.Headers["Location"], Is.EqualTo(url));
		}
	}
}