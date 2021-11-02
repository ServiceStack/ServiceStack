using NUnit.Framework;

namespace ServiceStack.Common.Tests;

public class JsonServiceClientTests
{
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
}