#if !NETFRAMEWORK

using System.Reflection;
using System.Threading;
using Funq;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace ServiceStack.WebHost.Endpoints.Tests;

public class NetCoreIocTests
{
    private readonly ServiceStackHost appHost = new AppHost().Init().Start(Config.ListeningOn);

    [OneTimeTearDown]
    public void OneTimeTearDown() => appHost.Dispose();
    
    class AppHost : AppSelfHostBase
    {
        public AppHost() : base(nameof(NetCoreIocTests), typeof(NetCoreIocTests).Assembly) {}

        public override void Configure(IServiceCollection services)
        {
            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            services.AddSingleton(c => new SingletonDep());
            services.AddScoped(c => new ScopedDep());
        }

        public override void Configure(Container container) {}
    }
    
    [Test]
    public void Does_resolve_scoped_deps_when()
    {
        var client = new JsonServiceClient(Config.ListeningOn);
        var response = client.Get(new NetCoreIocTest());
        Assert.That(response.SingletonDepCounter, Is.EqualTo(1));
        Assert.That(response.ScopedDepCounter, Is.EqualTo(1));
        
        response = client.Get(new NetCoreIocTest());
        Assert.That(response.SingletonDepCounter, Is.EqualTo(1));
        Assert.That(response.ScopedDepCounter, Is.EqualTo(2));
    }
}

public class NetCoreIocTest : IReturn<NetCoreIocTestResponse> {}

public class NetCoreIocTestResponse
{
    public int SingletonDepCounter { get; set; }
    public int ScopedDepCounter { get; set; }
}

public class SingletonDep
{
    public static int Counter;
    public SingletonDep() => Interlocked.Increment(ref Counter);
}
public class ScopedDep
{
    public static int Counter;
    public ScopedDep() => Interlocked.Increment(ref Counter);
}

public class NetCoreScopedTestServices : Service
{
    public SingletonDep Singleton { get; set; }
    public ScopedDep Scoped { get; set; }
    
    public object Any(NetCoreIocTest request) => new NetCoreIocTestResponse {
        SingletonDepCounter = SingletonDep.Counter,
        ScopedDepCounter = ScopedDep.Counter
    };
}

#endif