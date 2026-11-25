#if NETCORE
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using ServiceStack.Host;
using ServiceStack.Logging;
using ServiceStack.Messaging;
using ServiceStack.NetCore;
using ServiceStack.Testing;
using ServiceStack.Web;

namespace ServiceStack.WebHost.Endpoints.Tests;

public class InProcessServiceGatewayContextTests
{
    private readonly HttpContextAccessor accessor = new();
    private ServiceProvider provider;
    private BasicAppHost appHost;
    private ScopedDependency rejectedDependency;

    [SetUp]
    public void SetUp()
    {
        provider = new ServiceCollection()
            .AddSingleton<IHttpContextAccessor>(accessor)
            .AddScoped<ScopedDependency>()
            .AddTransient<ContextService>()
            .BuildServiceProvider();
        appHost = new TestAppHost(accessor);
        appHost.ConfigureAppHost = host => host.RegisterService<ContextService>();
        appHost.ConfigureContainer = container => container.Adapter = new NetCoreContainerAdapter(provider);
        appHost.GatewayRequestFilters.Add((req, dto) =>
        {
            if (dto is InspectExecution { Reject: true })
            {
                Assert.That(HostContext.TryGetCurrentRequest(), Is.SameAs(req));
                rejectedDependency = req.TryResolve<ScopedDependency>();
                throw HttpError.Forbidden("blocked");
            }
        });
        appHost.Init();
    }

    [TearDown]
    public void TearDown()
    {
        accessor.HttpContext = null;
        appHost.Dispose();
        provider.Dispose();
    }

    private sealed class TestAppHost(IHttpContextAccessor accessor) : BasicAppHost
    {
        public override IRequest TryGetCurrentRequest() => accessor.GetOrCreateRequest();
    }

    [Test]
    public async Task Gateway_calls_get_disposed_scopes_independent_of_ambient_http([Values] bool sync, [Values] bool ambient)
    {
        using var httpScope = provider.CreateScope();
        var outer = ambient ? SetAmbientRequest(httpScope) : null;
        var first = GatewayRequest.Create(new BasicHttpRequest());
        var second = GatewayRequest.Create(new BasicHttpRequest());

        async Task<ExecutionResponse> Send(GatewayRequest request)
        {
            var gateway = new InProcessServiceGateway(request);
            return sync ? gateway.Send<ExecutionResponse>(new InspectExecution())
                : await gateway.SendAsync<ExecutionResponse>(new InspectExecution());
        }

        var results = await Task.WhenAll(Send(first), Send(second));

        Assert.That(results[0].Dependency, Is.Not.SameAs(results[1].Dependency));
        Assert.That(httpScope.ServiceProvider.GetRequiredService<ScopedDependency>().Used, Is.False);
        Assert.That(results.Select(x => x.Dependency.Disposed), Is.All.True);
        Assert.That(first.ServiceScope, Is.Null);
        Assert.That(second.ServiceScope, Is.Null);
        Assert.That(HostContext.TryGetCurrentRequest(), Is.SameAs(outer));

        var reused = await Send(first);
        Assert.That(reused.Dependency, Is.Not.SameAs(results[0].Dependency));
        Assert.That(reused.Dependency.Disposed, Is.True);
    }

    [Test]
    public void Gateway_only_disposes_its_own_scope_when_filter_rejects([Values] bool attachScope)
    {
        using var supplied = attachScope ? provider.CreateScope() : null;
        var request = new GatewayRequest { ServiceScope = supplied };
        var gateway = new InProcessServiceGateway(request);

        Assert.ThrowsAsync<HttpError>(() => gateway.SendAsync<ExecutionResponse>(new InspectExecution { Reject = true }));

        Assert.That(rejectedDependency, Is.Not.Null);
        Assert.That(rejectedDependency.Disposed, Is.EqualTo(!attachScope));
        if (attachScope)
            Assert.That(rejectedDependency, Is.SameAs(supplied.ServiceProvider.GetRequiredService<ScopedDependency>()));
        Assert.That(request.ServiceScope, Is.SameAs(supplied));
        Assert.That(HostContext.TryGetCurrentRequest(), Is.Null);
    }

    [Test]
    public async Task Nested_gateway_restores_the_executing_http_request()
    {
        using var httpScope = provider.CreateScope();
        var outer = SetAmbientRequest(httpScope);

        var response = (ExecutionResponse)await appHost.ServiceController.ExecuteAsync(new InspectExecution { Nested = true }, outer);

        Assert.That(response.Dependency, Is.SameAs(httpScope.ServiceProvider.GetRequiredService<ScopedDependency>()));
        Assert.That(response.Dependency.Disposed, Is.False);
        Assert.That(HostContext.TryGetCurrentRequest(), Is.SameAs(outer));
    }

    [Test]
    public async Task Mq_uses_its_executing_request_with_ambient_http()
    {
        using var httpScope = provider.CreateScope();
        var outer = SetAmbientRequest(httpScope);
        var message = new Message<InspectExecution>(new InspectExecution());
        var mqRequest = new BasicRequest(message);

        var response = (ExecutionResponse)await appHost.ServiceController.ExecuteMessageAsync(message, mqRequest);

        Assert.That(response.Dependency.Disposed, Is.True);
        Assert.That(HostContext.TryGetCurrentRequest(), Is.SameAs(outer));
    }

    [Test]
    public void Adapter_warns_once_on_fallback_and_disposes_it_with_the_adapter()
    {
        var originalLogFactory = LogManager.LogFactory;
        var logs = TestLogger.GetLogs();
        var offset = logs.Count;
        LogManager.LogFactory = new TestLogFactory();
        try
        {
            ScopedDependency dependency;
            using (var adapter = new NetCoreContainerAdapter(provider))
            {
                dependency = adapter.TryResolve<ScopedDependency>();
                Assert.That(adapter.Resolve<ScopedDependency>(), Is.SameAs(dependency));
                Assert.That(dependency.Disposed, Is.False);
            }

            Assert.That(dependency.Disposed, Is.True);
            Assert.That(logs.Skip(offset).Count(x => x.Key == TestLogger.Levels.WARN
                && x.Value.Contains("host-wide fallback scope")), Is.EqualTo(1));
        }
        finally
        {
            LogManager.LogFactory = originalLogFactory;
        }
    }

    private IRequest SetAmbientRequest(IServiceScope scope)
    {
        accessor.HttpContext = new DefaultHttpContext { RequestServices = scope.ServiceProvider };
        return accessor.GetOrCreateRequest();
    }

    public class InspectExecution : IReturn<ExecutionResponse>
    {
        public bool Reject { get; set; }
        public bool Nested { get; set; }
    }

    public class ExecutionResponse
    {
        public ScopedDependency Dependency { get; set; }
    }

    public class ScopedDependency : IDisposable
    {
        public bool Disposed { get; private set; }
        public bool Used { get; set; }
        public void Dispose() => Disposed = true;
    }

    public class ContextService(ScopedDependency dependency) : Service
    {
        private readonly IRequest constructorRequest = HostContext.TryGetCurrentRequest();

        public async Task<ExecutionResponse> Any(InspectExecution request)
        {
            await Task.Yield();
            Assert.That(constructorRequest, Is.SameAs(Request));
            Assert.That(HostContext.TryGetCurrentRequest(), Is.SameAs(Request));
            Assert.That(HostContext.TryResolve<ScopedDependency>(), Is.SameAs(dependency));
            dependency.Used = true;

            if (request.Nested)
            {
                var gateway = new InProcessServiceGateway(new GatewayRequest());
                var nested = await gateway.SendAsync<ExecutionResponse>(new InspectExecution());
                Assert.That(nested.Dependency, Is.Not.SameAs(dependency));
                Assert.That(nested.Dependency.Disposed, Is.True);
                Assert.That(HostContext.TryGetCurrentRequest(), Is.SameAs(Request));
            }

            return new ExecutionResponse { Dependency = dependency };
        }
    }
}
#endif
