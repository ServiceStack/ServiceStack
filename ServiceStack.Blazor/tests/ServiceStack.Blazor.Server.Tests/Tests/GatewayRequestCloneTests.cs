using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using ServiceStack;
using ServiceStack.Auth;
using ServiceStack.Blazor;
using ServiceStack.Host;
using ServiceStack.Testing;

namespace MyApp.Tests;

public class GatewayRequestCloneTests
{
    [Test]
    public void Clone_copies_current_items_and_scope()
    {
        using var provider = new ServiceCollection().BuildServiceProvider();
        using var scope = provider.CreateScope();
        var original = new BasicHttpRequest();
        original.SetItem("caller", "original");
        original.SetItem("removed", true);
        var request = GatewayRequest.Create(original);
        request.SetItem("caller", "gateway");
        request.Items.Remove("removed");
        request.ServiceScope = scope;

        var clone = (GatewayRequest)request.Clone();

        Assert.Multiple(() =>
        {
            Assert.That(clone.GetItem("caller"), Is.EqualTo("gateway"));
            Assert.That(clone.Items.ContainsKey("removed"), Is.False);
            Assert.That(clone.ServiceScope, Is.SameAs(scope));
            Assert.That(clone.OriginalRequest, Is.SameAs(original));
        });

        clone.Items.Clear();
        Assert.That(request.GetItem("caller"), Is.EqualTo("gateway"));
    }

    [Test]
    public async Task ManagedApiAsync_passes_gateway_context_to_the_service()
    {
        using var provider = new ServiceCollection().AddScoped<ScopedContext>().BuildServiceProvider();
        using var scope = provider.CreateScope();
        scope.ServiceProvider.GetRequiredService<ScopedContext>().Value = "from scope";

        using var appHost = new BasicAppHost
        {
            ConfigureAppHost = host => host.RegisterService<GatewayContextService>(),
        }.Init();

        var hostState = new HostState { Session = new AuthUserSession { UserName = "test-user" } };
        var request = hostState.ToGatewayRequest();
        request.SetItem("caller", "from factory");
        request.ServiceScope = scope;
        IServiceGateway gateway = new InProcessServiceGateway(request);

        for (var i = 0; i < 2; i++)
        {
            var result = await gateway.ManagedApiAsync(new ReadGatewayContext());
            Assert.That(result.Succeeded, Is.True, result.Error?.Message);
            var response = result.Response!;
            Assert.Multiple(() =>
            {
                Assert.That(response.Caller, Is.EqualTo("from factory"));
                Assert.That(response.UserName, Is.EqualTo("test-user"));
                Assert.That(response.ScopedValue, Is.EqualTo("from scope"));
            });
        }
    }

    public class ReadGatewayContext : IReturn<GatewayContextResponse>;

    public class GatewayContextResponse
    {
        public string? Caller { get; set; }
        public string? UserName { get; set; }
        public string? ScopedValue { get; set; }
    }

    public class ScopedContext
    {
        public string? Value { get; set; }
    }

    public class GatewayContextService : Service
    {
        public GatewayContextResponse Any(ReadGatewayContext request)
        {
            var req = Request!;
            var caller = req.GetItem("caller") as string;
            req.Items.Remove("caller");
            return new GatewayContextResponse
            {
                Caller = caller,
                UserName = GetSession().UserName,
                ScopedValue = req.TryResolve<ScopedContext>()?.Value,
            };
        }
    }
}
