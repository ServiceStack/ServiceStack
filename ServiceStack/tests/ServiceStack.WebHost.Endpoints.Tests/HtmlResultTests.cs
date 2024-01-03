using NUnit.Framework;
using ServiceStack.Common.Tests;
using ServiceStack.Testing;
using ServiceStack.Text;
using ServiceStack.Web;

namespace ServiceStack.WebHost.Endpoints.Tests;

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

			return new HttpResult(html, MimeTypes.Html)
			{
				Headers = { { "Location", url } },
			};
		}
	}

	[Test]
	public void Test_response_with_html_result()
	{
		var mockResponse = new MockHttpResponse();

		const string url = "http://www.servicestack.net";
		var htmlResult = Html.RedirectTo(url);

		var responseWasAutoHandled = mockResponse.WriteToResponse(htmlResult, "text/xml");

		Assert.That(responseWasAutoHandled.Result, Is.True);

		var expectedOutput = string.Format(
			"<html><head><meta http-equiv=\"refresh\" content=\"0;url={0}\"></head></html>", url);

		var writtenString = mockResponse.ReadAsString();
		Assert.That(writtenString, Is.EqualTo(expectedOutput));
		Assert.That(mockResponse.Headers["Location"], Is.EqualTo(url));
	}
}