using System.Text;
using Moq;
using NUnit.Framework;
using NUnit.Framework.SyntaxHelpers;
using ServiceStack.WebHost.Endpoints.Extensions;
using ServiceStack.WebHost.Endpoints.Results;
using ServiceStack.WebHost.Endpoints.Tests.Mocks;

namespace ServiceStack.WebHost.Endpoints.Tests
{
	[TestFixture]
	public class HtmlResultTests : TestBase
	{

		public static class Html
		{
			public static HtmlResult RedirectTo(string url)
			{
				var html = string.Format(
					"<html><head><meta http-equiv=\"refresh\" content=\"0;url={0}\"></head></html>",
					url);

				return new HtmlResult {
					Html = new StringBuilder(html),
					HttpHeaders = { { "Location", url } },
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