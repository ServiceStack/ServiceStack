using Funq;
using NUnit.Framework;
using ServiceStack.Model;

namespace ServiceStack.WebHost.Endpoints.Tests;

public interface IHasSharedProperty
{
    string SharedProperty { get; set; } 
}

[Route("/tenant/{TenantName}/resourceType1")]
public class ResourceType1 : IReturn<ResourceType1>, IHasSharedProperty
{
    public string TenantName { get; set; }

    public string SubResourceName { get; set; }

    public string Arg1 { get; set; }

    public string SharedProperty { get; set; }
}

[Route("/tenant/{TenantName}/resourceType2")]
public class ResourceType2 : IReturn<ResourceType2>, IHasSharedProperty
{
    public string TenantName { get; set; }

    public string SubResourceName { get; set; }

    public string Arg1 { get; set; }

    public string SharedProperty { get; set; }
}

public interface IClearLongId
{
    long Id { get; set; }
}

public class TypedResponseFilter1 : IReturn<TypedResponseFilter1>, IClearLongId
{
    public long Id { get; set; }
}

public class TypedFilterService : Service
{
    public object Any(ResourceType1 request)
    {
        return request;
    }

    public object Any(ResourceType2 request)
    {
        return request;
    }

    public object Any(TypedResponseFilter1 request)
    {
        return request;
    }
}

[TestFixture]
public class RequestTypedFilterTests
{
    public class TypedFilterAppHost : AppSelfHostBase
    {
        public TypedFilterAppHost() 
            : base("Typed Filters", typeof(TypedFilterService).Assembly)
        {
        }

        public override void Configure(Container container)
        {
            RegisterTypedRequestFilter<ResourceType1>((req, res, dto) =>
            {
                var route = req.GetRoute();
                if (route != null && route.Path == "/tenant/{TenantName}/resourceType1")
                {
                    dto.SubResourceName = "CustomResource";
                }
            });
            RegisterTypedRequestFilter<IHasSharedProperty>((req, res, dtoInterface) =>
            {
                dtoInterface.SharedProperty = "Is Shared";
            });
            RegisterTypedResponseFilter<IClearLongId>((req, res, dtoInterface) =>
            {
                dtoInterface.Id = 0;
            });
        }
    }

    ServiceStackHost appHost;

    [OneTimeSetUp]
    public void OnTestFixtureSetUp()
    {
        appHost = new TypedFilterAppHost()
            .Init()
            .Start(Config.ListeningOn);
    }

    [OneTimeTearDown]
    public void OnTestFixtureTearDown()
    {
        appHost.Dispose();
    }

    [Test]
    public void Can_modify_requestDto_with_TypedRequestFilter()
    {
        var client = new JsonServiceClient(Config.ListeningOn);
        var response = client.Get(new ResourceType1
        {
            Arg1 = "arg1",
            TenantName = "tennant"
        });

        Assert.That(response.Arg1, Is.EqualTo("arg1"));
        Assert.That(response.TenantName, Is.EqualTo("tennant"));
        Assert.That(response.SubResourceName, Is.EqualTo("CustomResource"));
    }

    [Test]
    public void Does_execute_requestDto_interfaces_typedfilters()
    {
        var client = new JsonServiceClient(Config.ListeningOn);

        var response1 = client.Get(new ResourceType1 { TenantName = "tennant" });
        Assert.That(response1.SharedProperty, Is.EqualTo("Is Shared"));

        var response2 = client.Get(new ResourceType2 { TenantName = "tennant" });
        Assert.That(response2.SharedProperty, Is.EqualTo("Is Shared"));
    }

    [Test]
    public void Does_execute_requestDto_interfaces_TypedResponsefilters()
    {
        var client = new JsonServiceClient(Config.ListeningOn);

        var response1 = client.Get(new TypedResponseFilter1 { Id = 1 });
        Assert.That(response1.Id, Is.EqualTo(0));
    }
}