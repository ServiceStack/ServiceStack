using System;
using System.Threading.Tasks;
using Funq;
using NUnit.Framework;
using ServiceStack.Text;
using ServiceStack.Web;

namespace ServiceStack.WebHost.Endpoints.Tests;

public interface ITestFilters
{
    bool GlobalRequestFilter { get; set; }
    bool ServiceRequestAttributeFilter { get; set; }
    bool ActionRequestAttributeFilter { get; set; }
    bool Service { get; set; }
    bool ActionResponseAttributeFilter { get; set; }
    bool ServiceResponseAttributeFilter { get; set; }
    bool GlobalResponseFilter { get; set; }
}

public class TestFiltersSync : IReturn<TestFiltersSync>, ITestFilters
{
    public bool GlobalRequestFilter { get; set; }
    public bool ServiceRequestAttributeFilter { get; set; }
    public bool ActionRequestAttributeFilter { get; set; }
    public bool Service { get; set; }
    public bool ActionResponseAttributeFilter { get; set; }
    public bool ServiceResponseAttributeFilter { get; set; }
    public bool GlobalResponseFilter { get; set; }
}

public class TestFiltersAsync : IReturn<TestFiltersAsync>, ITestFilters
{
    public bool GlobalRequestFilter { get; set; }
    public bool ServiceRequestAttributeFilter { get; set; }
    public bool ActionRequestAttributeFilter { get; set; }
    public bool Service { get; set; }
    public bool ActionResponseAttributeFilter { get; set; }
    public bool ServiceResponseAttributeFilter { get; set; }
    public bool GlobalResponseFilter { get; set; }
}

public class ServiceRequestFilterAttribute : RequestFilterAttribute, IDisposable
{
    public DisposableDependency Dep { get; set; }

    public override void Execute(IRequest req, IResponse res, object requestDto)
    {
        Dep.AssertNotDisposed();

        ((ITestFilters)requestDto).ServiceRequestAttributeFilter = true;
    }

    public void Dispose()
    {
        Dep.Dispose();
    }
}

public class ActionRequestFilterAttribute : RequestFilterAttribute, IDisposable
{
    public DisposableDependency Dep { get; set; }

    public override void Execute(IRequest req, IResponse res, object requestDto)
    {
        Dep.AssertNotDisposed();

        ((ITestFilters)requestDto).ActionRequestAttributeFilter = true;
    }

    public void Dispose()
    {
        Dep.Dispose();
    }
}

public class ServiceResponseFilterAttribute : ResponseFilterAttribute, IDisposable
{
    public DisposableDependency Dep { get; set; }

    public override void Execute(IRequest req, IResponse res, object responseDto)
    {
        Dep.AssertNotDisposed();

        ((ITestFilters)responseDto).ServiceResponseAttributeFilter = true;
    }

    public void Dispose()
    {
        Dep.Dispose();
    }
}

public class ActionResponseFilterAttribute : ResponseFilterAttribute, IDisposable
{
    public DisposableDependency Dep { get; set; }

    public override void Execute(IRequest req, IResponse res, object responseDto)
    {
        Dep.AssertNotDisposed();

        ((ITestFilters)responseDto).ActionResponseAttributeFilter = true;
    }

    public void Dispose()
    {
        Dep.Dispose();
    }
}

[ServiceRequestFilter]
[ServiceResponseFilter]
public class RequestPipelineService : Service
{
    public DisposableDependency Dep { get; set; }

    [ActionRequestFilter]
    [ActionResponseFilter]
    public object Any(TestFiltersSync request)
    {
        Dep.AssertNotDisposed();

        request.Service = true;

        return request;
    }

    [ActionRequestFilter]
    [ActionResponseFilter]
    public async Task<TestFiltersAsync> Any(TestFiltersAsync request)
    {
        await Task.Delay(100);

        Dep.AssertNotDisposed();

        return await Task.Factory.StartNew(() =>
        {
            Task.Delay(100);

            Dep.AssertNotDisposed();

            request.Service = true;
            return request;
        });
    }

    public override void Dispose()
    {
        base.Dispose();
        Dep.Dispose();
    }
}

public class DisposableDependency : IDisposable
{
    private bool isDisposed;

    public void AssertNotDisposed()
    {
        if (isDisposed)
            throw new ObjectDisposedException("DisposableDependency");
    }

    public void Dispose()
    {
        isDisposed = true;
    }
}

public class RequestPipelineAppHost : AppHostHttpListenerBase
{
    public RequestPipelineAppHost() : base(typeof(RequestPipelineTests).Name, typeof(RequestPipelineService).Assembly) { }

    public override void Configure(Container container)
    {
        container.Register(c => new DisposableDependency()).ReusedWithin(ReuseScope.None);

        this.GlobalRequestFilters.Add((req, res, dto) => ((ITestFilters)dto).GlobalRequestFilter = true);
        this.GlobalResponseFilters.Add((req, res, dto) => ((ITestFilters)dto).GlobalResponseFilter = true);
    }
}

[TestFixture]
public class RequestPipelineTests
{
    private ServiceStackHost appHost;

    [OneTimeSetUp]
    public void TestFixtureSetUp()
    {
        appHost = new RequestPipelineAppHost()
            .Init()
            .Start(Config.AbsoluteBaseUri);
    }

    [OneTimeTearDown]
    public void TestFixtureTearDown()
    {
        appHost.Dispose();
    }

    public bool AllFieldsTrue(ITestFilters dto)
    {
        if (!dto.GlobalRequestFilter)
            return false;
        if (!dto.ServiceRequestAttributeFilter)
            return false;
        if (!dto.ActionRequestAttributeFilter)
            return false;
        if (!dto.Service)
            return false;
        if (!dto.ActionResponseAttributeFilter)
            return false;
        if (!dto.ServiceResponseAttributeFilter)
            return false;
        if (!dto.GlobalResponseFilter)
            return false;

        return true;
    }

    [Test]
    public void Does_fire_all_filters_sync()
    {
        var client = new JsonServiceClient(Config.ServiceStackBaseUri);

        var response = client.Get(new TestFiltersSync());

        Assert.That(AllFieldsTrue(response), Is.True);
    }

    [Test]
    public async Task Does_fire_all_filters_async()
    {
        var client = new JsonServiceClient(Config.ServiceStackBaseUri);

        var response = await client.GetAsync(new TestFiltersAsync());

        Assert.That(AllFieldsTrue(response));
    }
}