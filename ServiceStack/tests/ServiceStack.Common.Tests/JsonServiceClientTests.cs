using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;

namespace ServiceStack.Common.Tests;

public class JsonServiceClientTests
{
    [Test]
    public void Does_set_BasePath_default_ServiceClient()
    {
        var client = new JsonServiceClient("https://example.org");
        Assert.That(client.SyncReplyBaseUri, Is.EqualTo("https://example.org/json/reply/"));
        Assert.That(client.AsyncOneWayBaseUri, Is.EqualTo("https://example.org/json/oneway/"));
    }
    
    [Test]
    public void Does_set_BasePath_default_HttpClient()
    {
        var client = new JsonServiceClient("https://example.org");
        Assert.That(client.SyncReplyBaseUri, Is.EqualTo("https://example.org/json/reply/"));
        Assert.That(client.AsyncOneWayBaseUri, Is.EqualTo("https://example.org/json/oneway/"));
    }
    
    [Test]
    public void Does_change_BasePath_ServiceClient()
    {
        var client = new JsonServiceClient("https://example.org") {
            UseBasePath = "/api"
        };
        Assert.That(client.SyncReplyBaseUri, Is.EqualTo("https://example.org/api/"));
        Assert.That(client.AsyncOneWayBaseUri, Is.EqualTo("https://example.org/api/"));

        client.UseBasePath = null;
        Assert.That(client.SyncReplyBaseUri, Is.EqualTo("https://example.org/json/reply/"));
        Assert.That(client.AsyncOneWayBaseUri, Is.EqualTo("https://example.org/json/oneway/"));
    }

    [Test]
    public void Does_change_BasePath_HttpClient()
    {
        var client = new JsonHttpClient("https://example.org") {
            UseBasePath = "/api"
        };
        Assert.That(client.SyncReplyBaseUri, Is.EqualTo("https://example.org/api/"));
        Assert.That(client.AsyncOneWayBaseUri, Is.EqualTo("https://example.org/api/"));

        client.UseBasePath = null;
        Assert.That(client.SyncReplyBaseUri, Is.EqualTo("https://example.org/json/reply/"));
        Assert.That(client.AsyncOneWayBaseUri, Is.EqualTo("https://example.org/json/oneway/"));
    }

#if NET6_0_OR_GREATER
    [Test]
    public void Does_change_BasePath_JsonApiClient()
    {
        var client = new JsonApiClient("https://example.org");
        Assert.That(client.SyncReplyBaseUri, Is.EqualTo("https://example.org/api/"));
        Assert.That(client.AsyncOneWayBaseUri, Is.EqualTo("https://example.org/api/"));
        
        client = new JsonApiClient("https://example.org") {
            UseBasePath = "/json/reply"
        };
        Assert.That(client.SyncReplyBaseUri, Is.EqualTo("https://example.org/json/reply/"));
        Assert.That(client.AsyncOneWayBaseUri, Is.EqualTo("https://example.org/json/reply/"));

        client.UseBasePath = "/api";
        Assert.That(client.SyncReplyBaseUri, Is.EqualTo("https://example.org/api/"));
        Assert.That(client.AsyncOneWayBaseUri, Is.EqualTo("https://example.org/api/"));

        client = new JsonApiClient("https://example.org")
            .Apply(c => c.UseBasePath = "/custom");
        Assert.That(client.SyncReplyBaseUri, Is.EqualTo("https://example.org/custom/"));
        Assert.That(client.AsyncOneWayBaseUri, Is.EqualTo("https://example.org/custom/"));
    }

    [Test]
    public async Task CachedApiClient_PatchAsync_delegates_to_Patch()
    {
        var handler = new TestHttpMessageHandler();
        var client = new JsonApiClient("https://example.org") { HttpMessageHandler = handler };
        var cachedClient = new CachedApiClient(client);

        await cachedClient.PatchAsync(new TestVoidRequest());
        Assert.That(handler.LastRequest?.Method, Is.EqualTo(new HttpMethod("PATCH")));
    }
#endif

    [Test]
    public async Task CachedHttpClient_PatchAsync_delegates_to_Patch()
    {
        var handler = new TestHttpMessageHandler();
        var client = new JsonHttpClient("https://example.org") { HttpMessageHandler = handler };
        var cachedClient = new CachedHttpClient(client);

        await cachedClient.PatchAsync(new TestVoidRequest());
        Assert.That(handler.LastRequest?.Method, Is.EqualTo(new HttpMethod("PATCH")));
    }

    [Test]
    public async Task JsonHttpClient_SendAsync_appends_query_correctly_when_url_already_has_params()
    {
        var handler = new TestHttpMessageHandler();
        var client = new JsonHttpClient("https://example.org") { HttpMessageHandler = handler };
        await client.SendAsync<string>(HttpMethods.Get, "https://example.org/api?existing=1", new TestQueryRequest { Extra = "foo" });
        Assert.That(handler.LastRequest?.RequestUri?.Query, Does.Contain("existing=1&extra=foo").Or.Contain("existing=1&Extra=foo"));
    }

    [Test]
    public void GetContentType_returns_content_type_from_Content_Headers()
    {
        var httpRes = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{}", System.Text.Encoding.UTF8, "application/json")
        };
        var contentType = httpRes.GetContentType();
        Assert.That(contentType, Does.StartWith("application/json"));
    }

    [Test]
    public void ToWebServiceException_captures_Content_Headers()
    {
        var httpRes = new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            ReasonPhrase = "Bad Request",
            Content = new StringContent("{\"message\":\"error\"}", System.Text.Encoding.UTF8, "application/json")
        };
        var webEx = JsonHttpClient.ToWebServiceException(httpRes, "{\"message\":\"error\"}", null);
        Assert.That(webEx.ResponseHeaders[HttpHeaders.ContentType], Does.StartWith("application/json"));
    }

    [Test]
    public void CachedHttpClient_OnExceptionFilter_does_not_throw_when_RequestMessage_is_null()
    {
        var client = new JsonHttpClient("https://example.org");
        var cachedClient = new CachedHttpClient(client);
        var httpRes = new HttpResponseMessage(HttpStatusCode.NotFound)
        {
            RequestMessage = null
        };

        Assert.DoesNotThrow(() => cachedClient.OnExceptionFilter(httpRes, "https://example.org/test", typeof(string)));
    }

    [Test]
    public void CachedHttpClient_OnResultsFilterResponse_does_not_throw_when_Content_is_null()
    {
        var client = new JsonHttpClient("https://example.org");
        var cachedClient = new CachedHttpClient(client);
        var httpRes = new HttpResponseMessage(HttpStatusCode.NoContent)
        {
            Content = null
        };

        Assert.DoesNotThrow(() => client.ResultsFilterResponse?.Invoke(httpRes, new object(), HttpMethods.Get, "https://example.org/test", new object()));
    }
}

public class TestVoidRequest : IReturnVoid {}

public class TestQueryRequest
{
    public string Extra { get; set; }
}

public class TestHttpMessageHandler : HttpMessageHandler
{
    public HttpRequestMessage LastRequest { get; set; }
    public HttpResponseMessage ResponseToReturn { get; set; } = new(HttpStatusCode.OK)
    {
        Content = new StringContent("\"OK\"", System.Text.Encoding.UTF8, "application/json")
    };

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        LastRequest = request;
        return Task.FromResult(ResponseToReturn);
    }
}