#nullable enable
using System;
using System.Reflection;
using NUnit.Framework;
using ServiceStack;

namespace NetCoreTests;

[TestFixture]
public class KestrelHostSecurityTests
{
    private class TestKestrelAppHost : AppSelfHostBase
    {
        public TestKestrelAppHost() : base("TestKestrel", typeof(TestKestrelAppHost).Assembly)
        {
        }

        public override void Configure(Funq.Container container)
        {
        }
    }

    [Test]
    public void ParsePathBase_ExtractsPathBase_ForStandardUrl()
    {
        var appHost = new TestKestrelAppHost();
        var baseUrl = appHost.ParsePathBase("http://localhost:5000/api");
        
        Assert.That(baseUrl, Is.EqualTo("http://localhost:5000/"));
        Assert.That(appHost.PathBase, Is.EqualTo("/api"));
    }

    [Test]
    public void ParsePathBase_ExtractsPathBase_ForNestedPath()
    {
        var appHost = new TestKestrelAppHost();
        var baseUrl = appHost.ParsePathBase("https://my-domain.com:8443/services/v1");
        
        Assert.That(baseUrl, Is.EqualTo("https://my-domain.com:8443/"));
        Assert.That(appHost.PathBase, Is.EqualTo("/services/v1"));
    }

    [Test]
    public void ParsePathBase_DoesNotSetPathBase_ForRootOnlyUrls()
    {
        var appHost = new TestKestrelAppHost();
        
        var baseUrl1 = appHost.ParsePathBase("http://localhost:5000/");
        Assert.That(baseUrl1, Is.EqualTo("http://localhost:5000/"));
        Assert.That(appHost.PathBase, Is.Null);

        var baseUrl2 = appHost.ParsePathBase("http://localhost:5000");
        Assert.That(baseUrl2, Is.EqualTo("http://localhost:5000"));
        Assert.That(appHost.PathBase, Is.Null);
    }

    [Test]
    public void ParsePathBase_HandlesShortHostnamesAndEdgeCases_WithoutThrowing()
    {
        var appHost = new TestKestrelAppHost();
        
        // Previously "http://a" threw ArgumentOutOfRangeException because length was <= "https://".Length
        Assert.DoesNotThrow(() =>
        {
            var res = appHost.ParsePathBase("http://a");
            Assert.That(res, Is.EqualTo("http://a"));
        });

        Assert.DoesNotThrow(() =>
        {
            var res = appHost.ParsePathBase("http://a:80/sub");
            Assert.That(res, Is.EqualTo("http://a:80/"));
            Assert.That(appHost.PathBase, Is.EqualTo("/sub"));
        });

        Assert.DoesNotThrow(() =>
        {
            var res = appHost.ParsePathBase("");
            Assert.That(res, Is.Empty);
        });

        Assert.DoesNotThrow(() =>
        {
            var res = appHost.ParsePathBase("http://");
            Assert.That(res, Is.EqualTo("http://"));
        });
    }

    [Test]
    public void TryGetCurrentRequest_ReturnsNull_WhenAppNotBound()
    {
        var appHost = new TestKestrelAppHost();
        
        // Should safely return null without throwing NullReferenceException
        var req = appHost.TryGetCurrentRequest();
        Assert.That(req, Is.Null);
    }
}
