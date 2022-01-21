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
#endif
    
}