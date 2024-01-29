using System.IO;
using NUnit.Framework;
using ServiceStack.Common.Tests;
using ServiceStack.Host;
using ServiceStack.Testing;
using ServiceStack.Text;

namespace ServiceStack.WebHost.Endpoints.Tests;

[TestFixture]
public class HttpResultTests : TestBase
{
    protected override void Configure(Funq.Container container) { }

    [Test]
    public void Can_send_ResponseText_test_with_Custom_Header()
    {
        var mockResponse = new MockHttpResponse();

        var customText = "<h1>Custom Text</h1>";

        var httpResult = new HttpResult(customText, MimeTypes.Html)
        {
            Headers =
            {
                {"X-Custom","Header"}
            }
        };

        var responseWasAutoHandled = mockResponse.WriteToResponse(httpResult, MimeTypes.Html);

        Assert.That(responseWasAutoHandled.Result, Is.True);

        var writtenString = mockResponse.ReadAsString();
        Assert.That(writtenString, Is.EqualTo(customText));
        Assert.That(mockResponse.Headers["X-Custom"], Is.EqualTo("Header"));
    }

    [Test]
    public void Can_send_ResponseStream_test_with_Custom_Header()
    {
        var mockResponse = new MockHttpResponse();

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

        var responseWasAutoHandled = mockResponse.WriteToResponse(httpResult, MimeTypes.Html);

        Assert.That(responseWasAutoHandled.Result, Is.True);

        var writtenString = mockResponse.ReadAsString();
        Assert.That(writtenString, Is.EqualTo(customText));
        Assert.That(mockResponse.Headers["X-Custom"], Is.EqualTo("Header"));
    }

    [Test]
    public void Can_send_ResponseText_test_with_StatusDescription()
    {
        var mockRequest = new MockHttpRequest { ContentType = MimeTypes.Json };
        var mockResponse = mockRequest.Response;

        var customStatus = "Custom Status Description";

        var httpResult = new HttpResult(System.Net.HttpStatusCode.Accepted, customStatus)
        {
            RequestContext = mockRequest
        };

        var responseWasAutoHandled = mockResponse.WriteToResponse(httpResult, MimeTypes.Html);

        Assert.That(responseWasAutoHandled.Result, Is.True);

        var statusDesc = mockResponse.StatusDescription;
        Assert.That(mockResponse.StatusCode, Is.EqualTo((int)System.Net.HttpStatusCode.Accepted));
        Assert.That(statusDesc, Is.EqualTo(customStatus));
    }

    [Test]
    public void Can_handle_null_HttpResult_StatusDescription()
    {
        var mockResponse = new MockHttpResponse();

        var httpResult = new HttpResult { StatusDescription = null };

        var responseWasAutoHandled = mockResponse.WriteToResponse(httpResult, MimeTypes.Html);
        Assert.That(responseWasAutoHandled.Result, Is.True);

        Assert.IsNotNull(mockResponse.StatusDescription);
    }

    [Test]
    public void Can_change_serialization_options()
    {
        var mockResponse = new MockHttpResponse();

        var dto = new GetPoco();
        Assert.That(dto.ToJson(), Is.EqualTo("{}"));

        var httpResult = new HttpResult(dto)
        {
            ResultScope = () => JsConfig.With(new Text.Config { IncludeNullValues = true })
        };

        var responseWasAutoHandled = mockResponse.WriteToResponse(httpResult, MimeTypes.Html);
        Assert.That(responseWasAutoHandled.Result, Is.True);

        Assert.That(mockResponse.ReadAsString(), Is.EqualTo("{\"Text\":null}").Or.EqualTo("{\"text\":null}"));
    }

    [Test]
    public void Can_parse_ExtractHttpRanges()
    {
        void assertRange(long start, long expectedStart, long end, long expectedEnd)
        {
            Assert.That(start, Is.EqualTo(expectedStart));
            Assert.That(end, Is.EqualTo(expectedEnd));
        }
            
        "bytes=0-".ExtractHttpRanges(100, out var rangeStart, out var rangeEnd);
        assertRange(rangeStart, 0, rangeEnd, 99);
        "bytes=0-99".ExtractHttpRanges(100, out rangeStart, out rangeEnd);
        assertRange(rangeStart, 0, rangeEnd, 99);
        "bytes=1-2".ExtractHttpRanges(100, out rangeStart, out rangeEnd);
        assertRange(rangeStart, 1, rangeEnd, 2);
        "bytes=-50".ExtractHttpRanges(100, out rangeStart, out rangeEnd);
        assertRange(rangeStart, 49, rangeEnd, 99);

        Assert.Throws<HttpError>(() =>
            "".ExtractHttpRanges(100, out rangeStart, out rangeEnd));
        Assert.Throws<HttpError>(() =>
            "-100".ExtractHttpRanges(100, out rangeStart, out rangeEnd));
        Assert.Throws<HttpError>(() =>
            "0-10,10-20".ExtractHttpRanges(100, out rangeStart, out rangeEnd));
    }

}