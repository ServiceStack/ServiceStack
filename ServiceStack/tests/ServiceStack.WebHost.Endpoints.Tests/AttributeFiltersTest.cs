using System;
using System.Collections.Generic;
using System.Linq;
using ServiceStack.Caching;
using ServiceStack.Common.Tests.ServiceClient.Web;
using NUnit.Framework;
using ServiceStack.Support.WebHost;
using ServiceStack.Text;
using ServiceStack.Web;

namespace ServiceStack.WebHost.Endpoints.Tests;

//Always executed
public class FilterTestAttribute : AttributeBase, IHasRequestFilter
{
    private static ICacheClient previousCache;

    public ICacheClient Cache { get; set; }

    public int Priority { get; set; }

    public void RequestFilter(IRequest req, IResponse res, object requestDto)
    {
        var dto = (AttributeFiltered)requestDto;
        dto.AttrsExecuted.Add(GetType().Name);
        dto.RequestFilterExecuted = true;

        //Check for equality to previous cache to ensure a filter attribute is no singleton
        dto.RequestFilterDependencyIsResolved = Cache != null && !Cache.Equals(previousCache);

        previousCache = Cache;
    }

    public IRequestFilterBase Copy() => (IRequestFilterBase)this.MemberwiseClone();
}

//Only executed for the provided HTTP methods (GET, POST) 
public class ContextualFilterTestAttribute(ApplyTo applyTo) : RequestFilterAttribute(applyTo)
{
    public override void Execute(IRequest req, IResponse res, object requestDto)
    {
        var dto = (AttributeFiltered)requestDto;
        dto.AttrsExecuted.Add(GetType().Name);
        dto.ContextualRequestFilterExecuted = true;
    }
}

[Route("/attributefiltered")]
[FilterTest]
[ContextualFilterTest(ApplyTo.Delete | ApplyTo.Put)]
public class AttributeFiltered
{
    public bool RequestFilterExecuted { get; set; }
    public bool ContextualRequestFilterExecuted { get; set; }
    public bool InheritedRequestFilterExecuted { get; set; }
    public bool RequestFilterDependencyIsResolved { get; set; }
    public List<string> AttrsExecuted { get; set; } = [];
}

//Always executed
public class ResponseFilterTestAttribute : AttributeBase, IHasResponseFilter
{
    private static ICacheClient previousCache;

    public ICacheClient Cache { get; set; }

    public int Priority { get; set; }

    public void ResponseFilter(IRequest req, IResponse res, object response)
    {
        var dto = response.GetResponseDto() as AttributeFilteredResponse;
        dto.ResponseFilterExecuted = true;
        dto.ResponseFilterDependencyIsResolved = Cache != null && !Cache.Equals(previousCache);

        previousCache = Cache;
    }

    public IResponseFilterBase Copy() => (IResponseFilterBase)this.MemberwiseClone();
}

//Only executed for the provided HTTP methods (GET, POST) 
public class ContextualResponseFilterTestAttribute : ResponseFilterAttribute
{
    public ContextualResponseFilterTestAttribute(ApplyTo applyTo)
        : base(applyTo)
    {
    }

    public override void Execute(IRequest req, IResponse res, object responseDto)
    {
        var dto = responseDto as AttributeFilteredResponse;
        dto.ContextualResponseFilterExecuted = true;
    }
}

public class ThrowingFilterAttribute : RequestFilterAttribute
{
    public override void Execute(IRequest req, IResponse res, object requestDto)
    {
        throw new ArgumentException("exception message");
    }
}

[Route("/throwingattributefiltered")]
public class ThrowingAttributeFiltered : IReturn<string>
{
}

[ThrowingFilter]
public class ThrowingAttributeFilteredService : IService
{
    public object Any(ThrowingAttributeFiltered request)
    {
        return "OK";
    }
}

public class AttributeFilteredResponse
{
    public bool RequestFilterExecuted { get; set; }
    public bool InheritedRequestFilterExecuted { get; set; }
    public bool ContextualRequestFilterExecuted { get; set; }

    public bool ResponseFilterExecuted { get; set; }
    public bool ContextualResponseFilterExecuted { get; set; }
    public bool InheritedResponseFilterExecuted { get; set; }

    public bool RequestFilterDependencyIsResolved { get; set; }
    public bool ResponseFilterDependencyIsResolved { get; set; }

}

[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public class PriorityAttribute : RequestFilterAttribute
{
    public string Name { get; set; }

    public PriorityAttribute(int priority, string name)
    {
        Priority = priority;
        Name = name;
    }

    public override void Execute(IRequest req, IResponse res, object requestDto)
    {
        var dto = (PriorityAttributeTest)requestDto;
        dto.Names.Add(Name);
    }
}

public class PriorityAttributeTest : IReturn<PriorityAttributeTest>
{
    public List<string> Names { get; set; } = [];
}

public class PriorityAttributeService : Service
{
    [Priority(3, "3rd")]
    [Priority(2, "2nd")]
    [Priority(1, "1st")]
    public object Any(PriorityAttributeTest request)
    {
        return request;
    }
}

[InheritedRequestFilter]
[InheritedResponseFilter]
public class AttributeFilteredServiceBase : IService { }

public class AttributeAttributeFilteredService : AttributeFilteredServiceBase
{
    [ResponseFilterTest]
    [ContextualResponseFilterTest(ApplyTo.Delete | ApplyTo.Put)]
    public object Any(AttributeFiltered request)
    {
        return new AttributeFilteredResponse
        {
            ResponseFilterExecuted = false,
            ContextualResponseFilterExecuted = false,
            RequestFilterExecuted = request.RequestFilterExecuted,
            InheritedRequestFilterExecuted = request.InheritedRequestFilterExecuted,
            ContextualRequestFilterExecuted = request.ContextualRequestFilterExecuted,
            RequestFilterDependencyIsResolved = request.RequestFilterDependencyIsResolved,
            ResponseFilterDependencyIsResolved = false
        };
    }
}

public class InheritedRequestFilterAttribute : RequestFilterAttribute
{
    public override void Execute(IRequest req, IResponse res, object requestDto)
    {
        var dto = (AttributeFiltered)requestDto;
        dto.InheritedRequestFilterExecuted = true;
    }
}

public class InheritedResponseFilterAttribute : ResponseFilterAttribute
{
    public override void Execute(IRequest req, IResponse res, object responseDto)
    {
        var dto = (AttributeFilteredResponse)responseDto;
        dto.InheritedResponseFilterExecuted = true;
    }
}

[TestFixture]
public class AttributeFiltersTest
{
    private const string ListeningOn = Config.BaseUriHost;
    private const string ServiceClientBaseUri = Config.BaseUriHost;

    public class AttributeFiltersAppHostHttpListener() : AppHostHttpListenerBase("Attribute Filters Tests",
        typeof(AttributeAttributeFilteredService).Assembly)
    {
        public override void Configure(Funq.Container container)
        {
            container.Register<ICacheClient>(c => new MemoryCacheClient()).ReusedWithin(Funq.ReuseScope.None);
            SetConfig(new HostConfig { DebugMode = true }); //show stacktraces
        }
    }

    AttributeFiltersAppHostHttpListener appHost;

    [OneTimeSetUp]
    public void OnTestFixtureSetUp()
    {
        appHost = new AttributeFiltersAppHostHttpListener();
        appHost.Init();
        appHost.Start(ListeningOn);
    }

    [OneTimeTearDown]
    public void OnTestFixtureTearDown()
    {
        appHost.Dispose();
    }

    public IServiceClient GetServiceClient()
    {
        return new JsonServiceClient(ServiceClientBaseUri);
    }

    static IServiceClient[] ServiceClients =
    {
        new JsonServiceClient(ServiceClientBaseUri),
        new XmlServiceClient(ServiceClientBaseUri),
        new JsvServiceClient(ServiceClientBaseUri)
        //SOAP not supported in HttpListener
        //new Soap11ServiceClient(ServiceClientBaseUri),
        //new Soap12ServiceClient(ServiceClientBaseUri)
    };

    [Test, TestCaseSource("ServiceClients")]
    public void Request_and_Response_Filters_are_executed_using_ServiceClient(IServiceClient client)
    {
        var response = client.Send<AttributeFilteredResponse>(
            new AttributeFiltered { RequestFilterExecuted = false });
        Assert.IsTrue(response.RequestFilterExecuted);
        Assert.IsTrue(response.ResponseFilterExecuted);
        Assert.IsTrue(response.InheritedRequestFilterExecuted);
        Assert.IsTrue(response.InheritedResponseFilterExecuted);
        Assert.IsFalse(response.ContextualRequestFilterExecuted);
        Assert.IsFalse(response.ContextualResponseFilterExecuted);
        Assert.IsTrue(response.RequestFilterDependencyIsResolved);
        Assert.IsTrue(response.ResponseFilterDependencyIsResolved);
    }

    static IRestClient[] RestClients =
    {
        new JsonServiceClient(ServiceClientBaseUri),
        new XmlServiceClient(ServiceClientBaseUri),
        new JsvServiceClient(ServiceClientBaseUri)
    };

    [Test]
    public void Proper_exception_is_serialized_to_client()
    {
        var client = new HtmlServiceClient(ServiceClientBaseUri);
        client.SetBaseUri(ServiceClientBaseUri);

        try
        {
            client.Get(new ThrowingAttributeFiltered());
        }
        catch (WebServiceException e)
        {
            //Ensure we have stack trace present
            Assert.That(e.ResponseBody, Does.Contain("ThrowingFilterAttribute"), "No stack trace in the response (it's probably empty)");
        }
    }

    [Test, TestCaseSource("RestClients")]
    public void Request_and_Response_Filters_are_executed_using_RestClient(IRestClient client)
    {
        var response = client.Post<AttributeFilteredResponse>("attributefiltered", new AttributeFiltered() { RequestFilterExecuted = false });
        Assert.IsTrue(response.RequestFilterExecuted);
        Assert.IsTrue(response.ResponseFilterExecuted);
        Assert.IsFalse(response.ContextualRequestFilterExecuted);
        Assert.IsFalse(response.ContextualResponseFilterExecuted);
        Assert.IsTrue(response.RequestFilterDependencyIsResolved);
        Assert.IsTrue(response.ResponseFilterDependencyIsResolved);
    }

    [Test, TestCaseSource("RestClients")]
    public void Contextual_Request_and_Response_Filters_are_executed_using_RestClient(IRestClient client)
    {
        var response = client.Delete<AttributeFilteredResponse>("attributefiltered");
        Assert.IsTrue(response.RequestFilterExecuted);
        Assert.IsTrue(response.ResponseFilterExecuted);
        Assert.IsTrue(response.ContextualRequestFilterExecuted);
        Assert.IsTrue(response.ContextualResponseFilterExecuted);
        Assert.IsTrue(response.RequestFilterDependencyIsResolved);
        Assert.IsTrue(response.ResponseFilterDependencyIsResolved);
    }

    [Test, TestCaseSource("RestClients")]
    public void Multi_Contextual_Request_and_Response_Filters_are_executed_using_RestClient(IRestClient client)
    {
        var response = client.Put<AttributeFilteredResponse>("attributefiltered", new AttributeFiltered() { RequestFilterExecuted = false });
        Assert.IsTrue(response.RequestFilterExecuted);
        Assert.IsTrue(response.ResponseFilterExecuted);
        Assert.IsTrue(response.ContextualRequestFilterExecuted);
        Assert.IsTrue(response.ContextualResponseFilterExecuted);
        Assert.IsTrue(response.RequestFilterDependencyIsResolved);
        Assert.IsTrue(response.ResponseFilterDependencyIsResolved);
    }

    [Test]
    public void Does_order_action_attributes_by_priority()
    {
        var client = GetServiceClient();

        var response = client.Post(new PriorityAttributeTest());

        response.PrintDump();

        Assert.That(response.Names, Is.EqualTo(new[] { "1st", "2nd", "3rd" }));
    }

    public class ExecutedFirstAttribute : RequestFilterAttribute
    {
        public ExecutedFirstAttribute()
        {
            Priority = int.MinValue;
        }

        public override void Execute(IRequest req, IResponse res, object requestDto)
        {
            var dto = (AttributeFiltered)requestDto;
            dto.AttrsExecuted.Add(GetType().Name);
        }
    }

    [ExecutedFirst]
    [FilterTest]
    [RequiredRole("test")]
    [Authenticate]
    [RequiredPermission("test")]
    public class DummyHolder { }

    [Test]
    public void RequestFilters_are_prioritized()
    {
        appHost.Metadata.Add(typeof(AttributeAttributeFilteredService), typeof(DummyHolder), null);

        var attributes = FilterAttributeCache.GetRequestFilterAttributes(typeof(DummyHolder));
        var attrPriorities = attributes.ToList().ConvertAll(x => x.Priority);
        Assert.That(attrPriorities, Is.EquivalentTo(new[] { int.MinValue, -100, -90, -80, 0, 0 }));

        var execOrder = new IRequestFilterBase[attributes.Length];
        var i = 0;
        for (; i < attributes.Length && attributes[i].Priority < 0; i++)
        {
            execOrder[i] = attributes[i];
            Console.WriteLine(attributes[i].Priority);
        }

        Console.WriteLine("break;");

        for (; i < attributes.Length; i++)
        {
            execOrder[i] = attributes[i];
            Console.WriteLine(attributes[i].Priority);
        }

        var execOrderPriorities = execOrder.ToList().ConvertAll(x => x.Priority);
        Assert.That(execOrderPriorities, Is.EquivalentTo(new[] { int.MinValue, -100, -90, -80, 0, 0 }));
    }
}