using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using NUnit.Framework;
using ServiceStack.Auth;
using ServiceStack.Host;
using ServiceStack.Testing;
using ServiceStack.Web;

namespace ServiceStack.WebHost.Endpoints.Tests;

[TestFixture]
public class ServiceStackModernizationTests
{
    [Test]
    public void TryResolve_WithNullResolver_ReturnsDefaultWithoutThrowing()
    {
        var req = new MockHttpRequest();
        var resolved = req.TryResolve<IDisposable>();
        Assert.That(resolved, Is.Null);
    }

    [Test]
    public void GetRuntimeConfig_ReturnsDefaultValue_WhenAppHostIsNull()
    {
        var req = new MockHttpRequest();
        var val = req.GetRuntimeConfig("CustomSetting", "DefaultFallback");
        Assert.That(val, Is.EqualTo("DefaultFallback"));
    }

    [Test]
    public void RegisterForDispose_WithMockRequest_DoesNotThrowInvalidCastException()
    {
        var req = new MockHttpRequest();
        using var stream = new MemoryStream();
        Assert.DoesNotThrow(() => req.RegisterForDispose(stream));
    }

    [Test]
    public void SessionAs_InTestMode_ResolvesMockSessionCorrectly()
    {
        var prevTestMode = HostContext.TestMode;
        try
        {
            HostContext.TestMode = true;

            var expectedSession = new AuthUserSession
            {
                UserAuthId = "user-123",
                UserName = "testuser"
            };

            var container = new Funq.Container();
            container.Register<IAuthSession>(expectedSession);

            var req = new MockHttpRequest { Resolver = container };
            var session = req.SessionAs<IAuthSession>();

            Assert.That(session, Is.Not.Null);
            Assert.That(session.UserAuthId, Is.EqualTo("user-123"));
            Assert.That(session.UserName, Is.EqualTo("testuser"));
        }
        finally
        {
            HostContext.TestMode = prevTestMode;
        }
    }

    [Test]
    public void IsSubclassOfRawGeneric_HandlesInterfacesAndPrimitivesWithoutNRE()
    {
        Assert.That(typeof(IDisposable).IsSubclassOfRawGeneric(typeof(List<>)), Is.False);
        Assert.That(typeof(int).IsSubclassOfRawGeneric(typeof(Nullable<>)), Is.False);
        Assert.That(typeof(List<string>).IsSubclassOfRawGeneric(typeof(List<>)), Is.True);
    }

    [Test]
    public void CommandsFeature_Median_CalculatesProperly_AndHandlesNullAndEmpty()
    {
        IEnumerable<int> nullList = null;
        Assert.That(nullList.Median(), Is.EqualTo(0));

        int[] empty = [];
        Assert.That(empty.Median(), Is.EqualTo(0));

        int[] single = [7];
        Assert.That(single.Median(), Is.EqualTo(7));

        int[] odd = [1, 5, 2];
        Assert.That(odd.Median(), Is.EqualTo(2));

        int[] even = [1, 2, 3, 4];
        Assert.That(even.Median(), Is.EqualTo(2.5));
    }

    [Test]
    public void DeleteCookie_WithNullResponse_DoesNotThrow()
    {
        var result = new HttpResult();
        var req = new MockHttpRequest();
        Assert.DoesNotThrow(() => result.DeleteCookie(req, "test_cookie"));
        Assert.That(result.Cookies.Count, Is.EqualTo(1));
        Assert.That(result.Cookies[0].Name, Is.EqualTo("test_cookie"));
    }

    [Test]
    public async Task MemoryServerEvents_ImplementsIAsyncDisposable()
    {
        var mse = new MemoryServerEvents();
        Assert.That(mse, Is.InstanceOf<IAsyncDisposable>());

        await ((IAsyncDisposable)mse).DisposeAsync();
    }

    [Test]
    public void HostContext_Reset_ClearsDefaultOperationNamespace()
    {
        HostContext.DefaultOperationNamespace = "custom_ns";
        Assert.That(HostContext.DefaultOperationNamespace, Is.EqualTo("custom_ns"));

        HostContext.Reset();
        Assert.That(HostContext.DefaultOperationNamespace, Is.Null);
    }
}
