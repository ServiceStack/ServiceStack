using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using NUnit.Framework;
using ServiceStack.Configuration;
using ServiceStack.Host;
using ServiceStack.Host.Handlers;
using ServiceStack.Text;
using ServiceStack.Web;

namespace ServiceStack.WebHost.Endpoints.Tests;

[TestFixture]
public class HostModernizationTests
{
    [Test]
    public void ContainerResolveCache_NullGuards()
    {
        var cache = new ContainerResolveCache();
        Assert.That(cache.CreateInstance(null, null), Is.Null);

        var resolver = new BasicResolver();
        Assert.That(cache.CreateInstance(resolver, null), Is.Null);
        Assert.That(cache.CreateInstance(null, typeof(string)), Is.Null);

#if NET8_0_OR_GREATER
        Assert.That(ContainerResolveCache.PopulateInstance(null, null), Is.Null);
        Assert.That(ContainerResolveCache.PopulateInstance(resolver, null), Is.Null);
#endif
    }

    [Test]
    public void InMemoryRollingRequestLogger_NullAndEdgeCaseGuards()
    {
        var logger = new InMemoryRollingRequestLogger(10);
        Assert.That(logger.GetLatestLogs(null), Is.Not.Null);
        Assert.That(logger.GetLatestLogs(-5), Is.Not.Null);
        Assert.That(logger.GetLatestLogs(0).Count, Is.EqualTo(0));

        // Test with null response
        var req = new BasicRequest();
        req.Response = null;
        Assert.DoesNotThrow(() => logger.Log(req, new object(), new object(), TimeSpan.FromMilliseconds(10)));

        // ExcludeResponseTypes null check
        logger.ExcludeResponseTypes = null;
        Assert.DoesNotThrow(() => logger.Log(new BasicRequest(), new object(), new object(), TimeSpan.FromMilliseconds(5)));
    }

    [Test]
    public void RestPath_NullGuards_AndLiteralHashing()
    {
        Assert.That(RestPath.GetPathPartsForMatching(null), Is.EqualTo(TypeConstants.EmptyStringArray));
        Assert.That(RestPath.GetPathPartsForMatching(""), Is.EqualTo(TypeConstants.EmptyStringArray));

        var path1 = new RestPath(typeof(string), "/users/{Id}");
        var path2 = new RestPath(typeof(string), "/users/{Id}");
        Assert.That(path1.GetHashCode(), Is.EqualTo(path2.GetHashCode()));
        Assert.That(path1.UniqueMatchHashKey, Is.EqualTo(path2.UniqueMatchHashKey));

        Assert.That(path1.IsVariable(null), Is.False);
        Assert.That(path1.IsVariable("Id"), Is.True);
        Assert.That(path1.IsVariable("NonExistent"), Is.False);
    }

    [Test]
    public void ContentTypes_CanSerializeWithoutAppHost()
    {
        var serializers = ContentTypes.Instance.ContentTypeSerializers;
        Assert.That(serializers.ContainsKey(MimeTypes.Json), Is.True);

        using var ms = new MemoryStream();
        var testDto = new { Name = "ServiceStack", Version = 8 };
        // Should not throw even when HostContext.AppHost is null
        Assert.DoesNotThrow(() => serializers[MimeTypes.Json](null, testDto, ms));
        Assert.That(ms.Length, Is.GreaterThan(0));

        ms.Position = 0;
        var deserializers = ContentTypes.Instance.ContentTypeDeserializers;
        Assert.That(deserializers.ContainsKey(MimeTypes.Json), Is.True);
        var result = deserializers[MimeTypes.Json](typeof(Dictionary<string, object>), ms) as Dictionary<string, object>;
        Assert.That(result, Is.Not.Null);
        Assert.That(result["Name"]?.ToString(), Is.EqualTo("ServiceStack"));
    }

    [Test]
    public void ServiceController_NullGuards()
    {
        Assert.That(ServiceController.IsRequestType(null), Is.False);
        Assert.That(ServiceController.IsServiceType(null), Is.False);
        Assert.That(ServiceController.IsServiceAction((ActionMethod)null), Is.False);
        Assert.That(ServiceController.IsServiceAction((string)null), Is.False);
        Assert.That(ServiceController.GetServiceRequestTypes(null), Is.Empty);
        Assert.That(ServiceController.GetAutoBatchedRequestTypes(null), Is.Empty);
    }

    [Test]
    public void BasicRequest_And_BasicResponse_Robustness()
    {
        var req = new BasicRequest();
        Assert.That(req.GetService(null), Is.Null);
        Assert.That(req.GetHeader(null), Is.Null);
        Assert.That(req.GetHeader("NonExistent"), Is.Null);

        req.Headers = null;
        Assert.That(req.GetHeader("Test"), Is.Null);
        Assert.That(req.Authorization, Is.Null);
        req.Authorization = "Bearer token123";
        Assert.That(req.Authorization, Is.EqualTo("Bearer token123"));

        Assert.DoesNotThrow(() => req.PopulateWith(null));

        var res = new BasicResponse(req);
        Assert.That(res.GetHeader(null), Is.Null);
        Assert.DoesNotThrow(() => res.AddHeader(null, "val"));
        Assert.DoesNotThrow(() => res.RemoveHeader(null));

        // Test lazy OutputStream init in Write
        Assert.DoesNotThrow(() => res.Write("Hello World"));
        Assert.That(res.OutputStream.Length, Is.GreaterThan(0));
        Assert.DoesNotThrow(() => res.Write(null));
    }

    [Test]
    public void CustomActionHandler_ThrowsArgumentNull()
    {
        Assert.Throws<ArgumentNullException>(() => new CustomActionHandler(null));
        Assert.Throws<ArgumentNullException>(() => new CustomActionHandlerAsync(null));
    }

    [Test]
    public void CustomResponseHandler_ThrowsArgumentNull()
    {
        Assert.Throws<ArgumentNullException>(() => new CustomResponseHandler(null));
        Assert.Throws<ArgumentNullException>(() => new CustomResponseHandlerAsync(null));
    }

    [Test]
    public async Task NotFoundHttpHandler_NullGuards()
    {
        var handler = new NotFoundHttpHandler();
        var task = handler.ProcessRequestAsync(null, null, null);
        await task;
        Assert.That(task.IsCompletedSuccessfully, Is.True);
    }

    [Test]
    public async Task ForbiddenHttpHandler_NullGuards()
    {
        var handler = new ForbiddenHttpHandler();
        var task = handler.ProcessRequestAsync(null, null, null);
        await task;
        Assert.That(task.IsCompletedSuccessfully, Is.True);
    }

    [Test]
    public void Cookies_Extensions_StandAloneWithoutAppHost()
    {
        var cookie = new Cookie("ss-id", "12345", "/");
        Assert.DoesNotThrow(() => {
            var header = cookie.AsHeaderValue();
            Assert.That(header, Does.Contain("ss-id=12345"));
        });

#if NETCORE
        Assert.DoesNotThrow(() => {
            var opts = cookie.ToCookieOptions();
            Assert.That(opts.Path, Is.EqualTo("/"));
        });

        Assert.Throws<ArgumentNullException>(() => ((Cookie)null).ToCookieOptions());
#endif

        Assert.That(((Cookie)null).AsHeaderValue(), Is.Null);
    }

    [Test]
    public void HttpFile_NullGuards()
    {
        Assert.Throws<ArgumentNullException>(() => new HttpFile((IHttpFile)null));
    }

    private class BasicResolver : IResolver
    {
        public T TryResolve<T>() => default;
    }
}
